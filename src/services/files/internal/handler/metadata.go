package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/services/files/internal/service"
)

func (h *Handler) getFileMeta(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	file, err := h.fileService.Get(ctx, fileID)
	if err != nil {
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	writeJSON(w, http.StatusOK, file)
}

func (h *Handler) listFiles(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	ownerID := r.URL.Query().Get("owner")
	if ownerID == "" {
		writeError(w, http.StatusBadRequest, "owner query parameter is required")
		return
	}

	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))

	filesList, err := h.fileService.List(ctx, ownerID, limit, offset)
	if err != nil {
		slog.Error("failed to list files", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to list files")
		return
	}

	if limit <= 0 {
		limit = 50
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"files":  filesList,
		"limit":  limit,
		"offset": offset,
	})
}

func (h *Handler) deleteFile(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	if err := h.fileService.Delete(ctx, fileID); err != nil {
		slog.Error("failed to delete file", "error", err)
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	w.WriteHeader(http.StatusNoContent)
}

func (h *Handler) listVariants(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	variants, err := h.variantService.List(ctx, fileID)
	if err != nil {
		slog.Error("failed to list variants", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to list variants")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"variants": variants,
	})
}

type CreateVariantRequest struct {
	VariantType string `json:"variant_type"`
	ObjectKey   string `json:"object_key"`
	MimeType    string `json:"mime_type"`
	SizeBytes   int64  `json:"size_bytes"`
}

func (h *Handler) createVariant(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	fileID := r.PathValue("id")

	var req CreateVariantRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	variant, err := h.variantService.Create(ctx, service.CreateParams{
		FileID:      fileID,
		VariantType: req.VariantType,
		ObjectKey:   req.ObjectKey,
		MimeType:    req.MimeType,
		SizeBytes:   req.SizeBytes,
	})
	if err != nil {
		slog.Error("failed to create variant", "error", err)
		writeError(w, http.StatusNotFound, "file not found")
		return
	}

	writeJSON(w, http.StatusCreated, variant)
}
