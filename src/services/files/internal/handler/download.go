package handler

import (
	"io"
	"log/slog"
	"net/http"
	"time"
)

func (h *Handler) downloadFile(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	// Get file metadata
	file, err := h.store.GetFile(ctx, fileID)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	// Get file from storage
	obj, err := h.storage.Download(ctx, file.ObjectKey)
	if err != nil {
		slog.Error("failed to download from storage", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to download file")
		return
	}
	defer obj.Close()

	// Set headers
	w.Header().Set("Content-Type", file.MimeType)
	w.Header().Set("Content-Disposition", "attachment; filename=\""+file.Filename+"\"")

	// Stream file to response
	if _, err := io.Copy(w, obj); err != nil {
		slog.Error("failed to stream file", "error", err)
	}
}

func (h *Handler) presignDownload(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	// Get file metadata
	file, err := h.store.GetFile(ctx, fileID)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	// Generate presigned URL (valid for 15 minutes)
	expiry := 15 * time.Minute
	presignedURL, err := h.storage.PresignedDownloadURL(ctx, file.ObjectKey, expiry)
	if err != nil {
		slog.Error("failed to generate presigned URL", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to generate download URL")
		return
	}

	writeJSON(w, http.StatusOK, map[string]string{
		"download_url": presignedURL.String(),
		"filename":     file.Filename,
		"expires_at":   time.Now().Add(expiry).UTC().Format(time.RFC3339),
	})
}

func (h *Handler) downloadVariant(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")
	variantType := r.PathValue("type")

	// Get variant metadata
	variant, err := h.store.GetVariant(ctx, fileID, variantType)
	if err != nil {
		writeError(w, http.StatusNotFound, "variant not found")
		return
	}

	// Get variant from storage
	obj, err := h.storage.Download(ctx, variant.ObjectKey)
	if err != nil {
		slog.Error("failed to download variant from storage", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to download variant")
		return
	}
	defer obj.Close()

	// Set headers
	w.Header().Set("Content-Type", variant.MimeType)

	// Stream variant to response
	if _, err := io.Copy(w, obj); err != nil {
		slog.Error("failed to stream variant", "error", err)
	}
}
