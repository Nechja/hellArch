package handler

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"path/filepath"
	"time"

	"github.com/google/uuid"
	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/files"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
)

type PresignUploadRequest struct {
	Filename string `json:"filename"`
	MimeType string `json:"mime_type"`
	Size     int64  `json:"size"`
	OwnerID  string `json:"owner_id"`
}

type PresignUploadResponse struct {
	FileID    string `json:"file_id"`
	UploadURL string `json:"upload_url"`
	ObjectKey string `json:"object_key"`
	ExpiresAt string `json:"expires_at"`
}

func (h *Handler) uploadFile(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	// Limit request body size
	r.Body = http.MaxBytesReader(w, r.Body, h.maxUploadSize)

	if err := r.ParseMultipartForm(32 << 20); err != nil { // 32MB buffer
		writeError(w, http.StatusBadRequest, "failed to parse multipart form")
		return
	}

	file, header, err := r.FormFile("file")
	if err != nil {
		writeError(w, http.StatusBadRequest, "file is required")
		return
	}
	defer file.Close()

	ownerID := r.FormValue("owner_id")
	if ownerID == "" {
		writeError(w, http.StatusBadRequest, "owner_id is required")
		return
	}

	// Generate object key
	ext := filepath.Ext(header.Filename)
	objectKey := fmt.Sprintf("%s/%s%s", ownerID, uuid.New().String(), ext)

	// Calculate checksum while reading
	hasher := sha256.New()
	teeReader := io.TeeReader(file, hasher)

	// Upload to MinIO
	_, err = h.storage.Upload(ctx, storage.UploadParams{
		ObjectKey:   objectKey,
		Reader:      teeReader,
		Size:        header.Size,
		ContentType: header.Header.Get("Content-Type"),
	})
	if err != nil {
		slog.Error("failed to upload to storage", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to upload file")
		return
	}

	checksum := hex.EncodeToString(hasher.Sum(nil))

	// Get content type
	contentType := header.Header.Get("Content-Type")
	if contentType == "" {
		contentType = "application/octet-stream"
	}

	// Save metadata to database
	storedFile, err := h.store.CreateFile(ctx, store.CreateFileParams{
		OwnerID:        ownerID,
		Bucket:         h.storage.Bucket(),
		ObjectKey:      objectKey,
		Filename:       header.Filename,
		MimeType:       contentType,
		SizeBytes:      header.Size,
		ChecksumSha256: &checksum,
	})
	if err != nil {
		slog.Error("failed to save file metadata", "error", err)
		// Try to clean up the uploaded file
		h.storage.Delete(ctx, objectKey)
		writeError(w, http.StatusInternalServerError, "failed to save file metadata")
		return
	}

	// Publish event
	if h.publisher != nil {
		event := files.FileUploaded{
			FileID:         storedFile.ID,
			Filename:       storedFile.Filename,
			MimeType:       storedFile.MimeType,
			SizeBytes:      storedFile.SizeBytes,
			OwnerID:        storedFile.OwnerID,
			ChecksumSha256: storedFile.ChecksumSha256,
			UploadedAt:     storedFile.CreatedAt,
		}
		if err := h.publisher.Publish(ctx, cloudevents.EventTypeFileUploaded, event); err != nil {
			slog.Warn("failed to publish file uploaded event", "error", err)
		}
	}

	writeJSON(w, http.StatusCreated, storedFile)
}

func (h *Handler) presignUpload(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	var req PresignUploadRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	if req.Filename == "" || req.OwnerID == "" {
		writeError(w, http.StatusBadRequest, "filename and owner_id are required")
		return
	}

	if req.Size > h.maxUploadSize {
		writeError(w, http.StatusBadRequest, fmt.Sprintf("file size exceeds maximum of %d bytes", h.maxUploadSize))
		return
	}

	// Generate object key
	ext := filepath.Ext(req.Filename)
	fileID := uuid.New().String()
	objectKey := fmt.Sprintf("%s/%s%s", req.OwnerID, fileID, ext)

	// Generate presigned URL (valid for 15 minutes)
	expiry := 15 * time.Minute
	presignedURL, err := h.storage.PresignedUploadURL(ctx, objectKey, expiry)
	if err != nil {
		slog.Error("failed to generate presigned URL", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to generate upload URL")
		return
	}

	writeJSON(w, http.StatusOK, PresignUploadResponse{
		FileID:    fileID,
		UploadURL: presignedURL.String(),
		ObjectKey: objectKey,
		ExpiresAt: time.Now().Add(expiry).UTC().Format(time.RFC3339),
	})
}

type ConfirmUploadRequest struct {
	ObjectKey string `json:"object_key"`
	Filename  string `json:"filename"`
	MimeType  string `json:"mime_type"`
	OwnerID   string `json:"owner_id"`
}

func (h *Handler) confirmUpload(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	var req ConfirmUploadRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	// Verify the object exists in MinIO
	info, err := h.storage.Stat(ctx, req.ObjectKey)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found in storage")
		return
	}

	// Save metadata to database
	storedFile, err := h.store.CreateFile(ctx, store.CreateFileParams{
		OwnerID:   req.OwnerID,
		Bucket:    h.storage.Bucket(),
		ObjectKey: req.ObjectKey,
		Filename:  req.Filename,
		MimeType:  req.MimeType,
		SizeBytes: info.Size,
	})
	if err != nil {
		slog.Error("failed to save file metadata", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to save file metadata")
		return
	}

	// Publish event
	if h.publisher != nil {
		event := files.FileUploaded{
			FileID:     storedFile.ID,
			Filename:   storedFile.Filename,
			MimeType:   storedFile.MimeType,
			SizeBytes:  storedFile.SizeBytes,
			OwnerID:    storedFile.OwnerID,
			UploadedAt: storedFile.CreatedAt,
		}
		if err := h.publisher.Publish(ctx, cloudevents.EventTypeFileUploaded, event); err != nil {
			slog.Warn("failed to publish file uploaded event", "error", err)
		}
	}

	writeJSON(w, http.StatusCreated, storedFile)
}
