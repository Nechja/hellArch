package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/services/events/internal/store"
)

type AuditHandler struct {
	store *store.Store
}

func NewAuditHandler(store *store.Store) *AuditHandler {
	return &AuditHandler{store: store}
}

type createAuditLogRequest struct {
	EventType string          `json:"event_type"`
	EventID   string          `json:"event_id"`
	Source    string          `json:"source"`
	ActorID   *string         `json:"actor_id,omitempty"`
	TargetID  *string         `json:"target_id,omitempty"`
	Metadata  json.RawMessage `json:"metadata,omitempty"`
}

func (h *AuditHandler) listAuditLogs(w http.ResponseWriter, r *http.Request) {
	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))
	eventType := r.URL.Query().Get("event_type")
	actorID := r.URL.Query().Get("actor_id")

	params := store.ListAuditLogsParams{
		Limit:     limit,
		Offset:    offset,
		EventType: eventType,
		ActorID:   actorID,
	}

	logs, total, err := h.store.ListAuditLogs(r.Context(), params)
	if err != nil {
		slog.Error("failed to list audit logs", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to list audit logs")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"data":  logs,
		"total": total,
	})
}

func (h *AuditHandler) createAuditLog(w http.ResponseWriter, r *http.Request) {
	var req createAuditLogRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.EventType == "" || req.EventID == "" || req.Source == "" {
		writeError(w, http.StatusBadRequest, "event_type, event_id, and source are required")
		return
	}

	log := &store.AuditLog{
		EventType: req.EventType,
		EventID:   req.EventID,
		Source:    req.Source,
		ActorID:   req.ActorID,
		TargetID:  req.TargetID,
		Metadata:  req.Metadata,
	}

	if err := h.store.CreateAuditLog(r.Context(), log); err != nil {
		slog.Error("failed to create audit log", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to create audit log")
		return
	}

	writeJSON(w, http.StatusCreated, log)
}
