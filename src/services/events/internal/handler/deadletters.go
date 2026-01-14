package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/services/events/internal/store"
)

type DeadLetterHandler struct {
	store *store.Store
}

func NewDeadLetterHandler(store *store.Store) *DeadLetterHandler {
	return &DeadLetterHandler{store: store}
}

type CreateDeadLetterRequest struct {
	EventType     string          `json:"event_type"`
	EventID       string          `json:"event_id"`
	Source        string          `json:"source"`
	OriginalQueue string          `json:"original_queue"`
	DeathReason   string          `json:"death_reason"`
	RetryCount    int             `json:"retry_count"`
	Body          json.RawMessage `json:"body"`
	ErrorMessage  string          `json:"error_message,omitempty"`
}

func (h *DeadLetterHandler) listDeadLetters(w http.ResponseWriter, r *http.Request) {
	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))

	params := store.ListDeadLettersParams{
		Limit:     limit,
		Offset:    offset,
		EventType: r.URL.Query().Get("event_type"),
	}

	dls, total, err := h.store.ListDeadLetters(r.Context(), params)
	if err != nil {
		slog.Error("failed to list dead letters", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to list dead letters")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"dead_letters": dls,
		"total":        total,
		"limit":        params.Limit,
		"offset":       params.Offset,
	})
}

func (h *DeadLetterHandler) createDeadLetter(w http.ResponseWriter, r *http.Request) {
	var req CreateDeadLetterRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	var errorMessage *string
	if req.ErrorMessage != "" {
		errorMessage = &req.ErrorMessage
	}

	dl := &store.DeadLetter{
		EventType:     req.EventType,
		EventID:       req.EventID,
		Source:        req.Source,
		OriginalQueue: req.OriginalQueue,
		DeathReason:   req.DeathReason,
		RetryCount:    req.RetryCount,
		Body:          string(req.Body),
		ErrorMessage:  errorMessage,
	}

	if err := h.store.CreateDeadLetter(r.Context(), dl); err != nil {
		slog.Error("failed to create dead letter", "error", err)
		writeError(w, http.StatusInternalServerError, "failed to create dead letter")
		return
	}

	slog.Warn("dead letter stored",
		"event_type", req.EventType,
		"event_id", req.EventID,
		"original_queue", req.OriginalQueue,
		"death_reason", req.DeathReason,
		"retry_count", req.RetryCount)

	writeJSON(w, http.StatusCreated, dl)
}

func (h *DeadLetterHandler) deleteDeadLetter(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")
	if id == "" {
		writeError(w, http.StatusBadRequest, "id is required")
		return
	}

	if err := h.store.DeleteDeadLetter(r.Context(), id); err != nil {
		slog.Error("failed to delete dead letter", "error", err, "id", id)
		writeError(w, http.StatusInternalServerError, "failed to delete dead letter")
		return
	}

	w.WriteHeader(http.StatusNoContent)
}
