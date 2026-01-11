package service

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"io"
	"log/slog"
	"mime/multipart"
	"net/url"
	"path/filepath"
	"time"

	"github.com/google/uuid"
	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/files"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
)

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

	hasher := sha256.New()
	teeReader := io.TeeReader(params.File, hasher)

	contentType := params.ContentType
	if contentType == "" {
		contentType = "application/octet-stream"
	}

	_, err := u.storage.Upload(ctx, storage.UploadParams{
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
