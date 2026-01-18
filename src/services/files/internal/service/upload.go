package service

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"fmt"
	"io"
	"log/slog"
	"mime/multipart"
	"net/http"
	"net/url"
	"path/filepath"
	"strings"
	"time"

	"github.com/google/uuid"
	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/files"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
)

// ErrContentTypeMismatch is returned when the detected content type doesn't match the claimed type
var ErrContentTypeMismatch = errors.New("content type mismatch: detected type differs from claimed type")

type Upload struct {
	store         *store.Store
	storage       *storage.MinioStorage
	publisher     *rabbitmq.Publisher
	maxUploadSize int64
}

func NewUpload(
	store *store.Store,
	storage *storage.MinioStorage,
	publisher *rabbitmq.Publisher,
	maxUploadSize int64,
) *Upload {
	return &Upload{
		store:         store,
		storage:       storage,
		publisher:     publisher,
		maxUploadSize: maxUploadSize,
	}
}

type UploadParams struct {
	File        multipart.File
	Header      *multipart.FileHeader
	OwnerID     string
	ContentType string
}

func (u *Upload) Upload(ctx context.Context, params UploadParams) (*store.StoredFile, error) {
	ext := filepath.Ext(params.Header.Filename)
	objectKey := fmt.Sprintf("%s/%s%s", params.OwnerID, uuid.New().String(), ext)

	// Detect actual content type from file bytes
	detectedType, err := detectContentType(params.File)
	if err != nil {
		return nil, fmt.Errorf("failed to detect content type: %w", err)
	}

	// Validate claimed type against detected type
	contentType, err := validateContentType(params.ContentType, detectedType)
	if err != nil {
		return nil, err
	}

	hasher := sha256.New()
	teeReader := io.TeeReader(params.File, hasher)

	_, err = u.storage.Upload(ctx, storage.UploadParams{
		ObjectKey:   objectKey,
		Reader:      teeReader,
		Size:        params.Header.Size,
		ContentType: contentType,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to upload to storage: %w", err)
	}

	checksum := hex.EncodeToString(hasher.Sum(nil))

	storedFile, err := u.store.CreateFile(ctx, store.CreateFileParams{
		OwnerID:        params.OwnerID,
		Bucket:         u.storage.Bucket(),
		ObjectKey:      objectKey,
		Filename:       params.Header.Filename,
		MimeType:       contentType,
		SizeBytes:      params.Header.Size,
		ChecksumSha256: &checksum,
	})
	if err != nil {
		if delErr := u.storage.Delete(ctx, objectKey); delErr != nil {
			slog.Error("failed to cleanup uploaded file after db error", "error", delErr)
		}
		return nil, fmt.Errorf("failed to save file metadata: %w", err)
	}

	u.publishUploaded(ctx, storedFile)

	return storedFile, nil
}

type PresignResult struct {
	FileID    string
	UploadURL *url.URL
	ObjectKey string
	ExpiresAt time.Time
}

func (u *Upload) Presign(ctx context.Context, filename, ownerID string, size int64) (*PresignResult, error) {
	if size > u.maxUploadSize {
		return nil, fmt.Errorf("file size exceeds maximum of %d bytes", u.maxUploadSize)
	}

	ext := filepath.Ext(filename)
	fileID := uuid.New().String()
	objectKey := fmt.Sprintf("%s/%s%s", ownerID, fileID, ext)

	expiry := 15 * time.Minute
	presignedURL, err := u.storage.PresignedUploadURL(ctx, objectKey, expiry)
	if err != nil {
		return nil, fmt.Errorf("failed to generate presigned URL: %w", err)
	}

	return &PresignResult{
		FileID:    fileID,
		UploadURL: presignedURL,
		ObjectKey: objectKey,
		ExpiresAt: time.Now().Add(expiry).UTC(),
	}, nil
}

type ConfirmParams struct {
	ObjectKey string
	Filename  string
	MimeType  string
	OwnerID   string
}

func (u *Upload) Confirm(ctx context.Context, params ConfirmParams) (*store.StoredFile, error) {
	info, err := u.storage.Stat(ctx, params.ObjectKey)
	if err != nil {
		return nil, fmt.Errorf("file not found in storage: %w", err)
	}

	storedFile, err := u.store.CreateFile(ctx, store.CreateFileParams{
		OwnerID:   params.OwnerID,
		Bucket:    u.storage.Bucket(),
		ObjectKey: params.ObjectKey,
		Filename:  params.Filename,
		MimeType:  params.MimeType,
		SizeBytes: info.Size,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to save file metadata: %w", err)
	}

	u.publishUploaded(ctx, storedFile)

	return storedFile, nil
}

func (u *Upload) MaxUploadSize() int64 {
	return u.maxUploadSize
}

// detectContentType reads the first 512 bytes of a file to detect its actual content type.
// It returns the detected type and resets the file reader to the beginning.
func detectContentType(file multipart.File) (string, error) {
	// Read the first 512 bytes for content type detection
	buf := make([]byte, 512)
	n, err := file.Read(buf)
	if err != nil && err != io.EOF {
		return "", fmt.Errorf("failed to read file for content detection: %w", err)
	}

	// Reset file position to beginning
	if _, err := file.Seek(0, io.SeekStart); err != nil {
		return "", fmt.Errorf("failed to reset file position: %w", err)
	}

	// Detect content type from actual bytes
	return http.DetectContentType(buf[:n]), nil
}

// validateContentType compares the claimed content type against the detected type.
// Returns the validated content type to use, or an error if validation fails.
func validateContentType(claimed, detected string) (string, error) {
	// Normalize content types by removing parameters (e.g., "text/plain; charset=utf-8" -> "text/plain")
	claimedBase := strings.Split(claimed, ";")[0]
	claimedBase = strings.TrimSpace(strings.ToLower(claimedBase))

	detectedBase := strings.Split(detected, ";")[0]
	detectedBase = strings.TrimSpace(strings.ToLower(detectedBase))

	// If no claimed type, use detected
	if claimedBase == "" || claimedBase == "application/octet-stream" {
		return detected, nil
	}

	// application/octet-stream from detection means the type couldn't be determined
	// In this case, trust the claimed type for uncommon file types
	if detectedBase == "application/octet-stream" {
		return claimed, nil
	}

	// Check for dangerous mismatches: claimed safe type but detected executable/script
	dangerousTypes := []string{
		"application/x-executable",
		"application/x-msdos-program",
		"application/x-msdownload",
	}

	for _, dangerous := range dangerousTypes {
		if detectedBase == dangerous && claimedBase != dangerous {
			slog.Warn("blocked dangerous content type mismatch",
				"claimed", claimed, "detected", detected)
			return "", ErrContentTypeMismatch
		}
	}

	// For images, ensure the detected type matches the claimed type
	// This prevents uploading executables disguised as images
	if strings.HasPrefix(claimedBase, "image/") && !strings.HasPrefix(detectedBase, "image/") {
		slog.Warn("blocked image content type mismatch",
			"claimed", claimed, "detected", detected)
		return "", ErrContentTypeMismatch
	}

	// Use the detected type as it's more reliable
	return detected, nil
}

func (u *Upload) publishUploaded(ctx context.Context, file *store.StoredFile) {
	if u.publisher == nil {
		return
	}

	event := files.FileUploaded{
		FileID:         file.ID,
		Filename:       file.Filename,
		MimeType:       file.MimeType,
		SizeBytes:      file.SizeBytes,
		OwnerID:        file.OwnerID,
		ChecksumSha256: file.ChecksumSha256,
		UploadedAt:     file.CreatedAt,
	}
	if err := u.publisher.Publish(ctx, cloudevents.EventTypeFileUploaded, event); err != nil {
		slog.Warn("failed to publish file uploaded event", "error", err, "file_id", file.ID)
	}
}
