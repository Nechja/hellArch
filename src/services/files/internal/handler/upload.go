package handler

import (
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"time"

	"github.com/nechja/hellarch/services/files/internal/service"
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

	// Owner ID must come from Gateway (X-User-ID header), not from user input
	// This prevents users from uploading files as other users
	ownerID := r.Header.Get("X-User-ID")
	if ownerID == "" {
		writeError(w, http.StatusUnauthorized, "authentication required")
		return
	}

	r.Body = http.MaxBytesReader(w, r.Body, h.uploadService.MaxUploadSize())

	if err := r.ParseMultipartForm(32 << 20); err != nil {
		writeError(w, http.StatusBadRequest, "failed to parse multipart form")
		return
	}

	file, header, err := r.FormFile("file")
	if err != nil {
		writeError(w, http.StatusBadRequest, "file is required")
		return
	}
	defer file.Close()

	contentType := header.Header.Get("Content-Type")

	storedFile, err := h.uploadService.Upload(ctx, service.UploadParams{
		File:        file,
		Header:      header,
		OwnerID:     ownerID,
		ContentType: contentType,
	})
	if err != nil {
		if errors.Is(err, service.ErrContentTypeMismatch) {
			writeError(w, http.StatusBadRequest, "content type validation failed: file content does not match declared type")
			return
		}
		slog.Error("failed to upload file", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to upload file")
		return
	}

	writeJSON(w, http.StatusCreated, storedFile)
}

func (h *Handler) presignUpload(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	// Owner ID must come from Gateway (X-User-ID header), not from user input
	ownerID := r.Header.Get("X-User-ID")
	if ownerID == "" {
		writeError(w, http.StatusUnauthorized, "authentication required")
		return
	}

	var req PresignUploadRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	if req.Filename == "" {
		writeError(w, http.StatusBadRequest, "filename is required")
		return
	}

	result, err := h.uploadService.Presign(ctx, req.Filename, ownerID, req.Size)
	if err != nil {
		slog.Error("failed to generate presigned URL", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to generate upload URL")
		return
	}

	writeJSON(w, http.StatusOK, PresignUploadResponse{
		FileID:    result.FileID,
		UploadURL: result.UploadURL.String(),
		ObjectKey: result.ObjectKey,
		ExpiresAt: result.ExpiresAt.Format(time.RFC3339),
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

	// Owner ID must come from Gateway (X-User-ID header), not from user input
	ownerID := r.Header.Get("X-User-ID")
	if ownerID == "" {
		writeError(w, http.StatusUnauthorized, "authentication required")
		return
	}

	var req ConfirmUploadRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	storedFile, err := h.uploadService.Confirm(ctx, service.ConfirmParams{
		ObjectKey: req.ObjectKey,
		Filename:  req.Filename,
		MimeType:  req.MimeType,
		OwnerID:   ownerID,
	})
	if err != nil {
		slog.Error("failed to confirm upload", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to confirm upload")
		return
	}

	writeJSON(w, http.StatusCreated, storedFile)
}
