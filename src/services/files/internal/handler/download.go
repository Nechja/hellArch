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

	result, err := h.fileService.Download(ctx, fileID)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found")
		return
	}
	defer result.Reader.Close()

	w.Header().Set("Content-Type", result.File.MimeType)
	w.Header().Set("Content-Disposition", "attachment; filename=\""+result.File.Filename+"\"")

	if _, err := io.Copy(w, result.Reader); err != nil {
		slog.Error("failed to stream file", "error", err)
	}
}

func (h *Handler) presignDownload(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	result, err := h.fileService.PresignDownload(ctx, fileID)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	writeJSON(w, http.StatusOK, map[string]string{
		"download_url": result.URL.String(),
		"filename":     result.Filename,
		"expires_at":   result.ExpiresAt.Format(time.RFC3339),
	})
}

func (h *Handler) downloadVariant(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")
	variantType := r.PathValue("type")

	result, err := h.variantService.Download(ctx, fileID, variantType)
	if err != nil {
		writeError(w, http.StatusNotFound, "variant not found")
		return
	}
	defer result.Reader.Close()

	w.Header().Set("Content-Type", result.Variant.MimeType)

	if _, err := io.Copy(w, result.Reader); err != nil {
		slog.Error("failed to stream variant", "error", err)
	}
}
