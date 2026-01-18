package handler

import (
	"io"
	"log/slog"
	"net/http"
	"net/url"
	"path/filepath"
	"strings"
	"time"
)

// sanitizeFilename removes path components and dangerous characters from filenames
// to prevent path traversal and header injection attacks
func sanitizeFilename(filename string) string {
	// URL-decode to catch encoded traversal attempts like %2F
	if decoded, err := url.QueryUnescape(filename); err == nil {
		filename = decoded
	}

	// Normalize path separators before Base extraction
	filename = strings.ReplaceAll(filename, "\\", "/")

	// Extract just the base name (removes any path components)
	filename = filepath.Base(filename)

	// Remove any remaining path traversal sequences
	for strings.Contains(filename, "..") {
		filename = strings.ReplaceAll(filename, "..", "")
	}

	// Replace header injection characters
	filename = strings.ReplaceAll(filename, "\"", "")
	filename = strings.ReplaceAll(filename, "\n", "")
	filename = strings.ReplaceAll(filename, "\r", "")

	// If filename is empty or just dots after sanitization, use a safe default
	if filename == "" || filename == "." {
		filename = "download"
	}

	return filename
}

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
	w.Header().Set("Content-Disposition", "attachment; filename=\""+sanitizeFilename(result.File.Filename)+"\"")

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
