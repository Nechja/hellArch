package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/services/events/internal/store"
)

type DeliveryHandler struct {
	store *store.Store
}

func NewDeliveryHandler(store *store.Store) *DeliveryHandler {
	return &DeliveryHandler{store: store}
}

type createDeliveryRequest struct {
	WebhookID    string          `json:"webhook_id"`
	EventID      string          `json:"event_id"`
	EventType    string          `json:"event_type"`
	Payload      json.RawMessage `json:"payload,omitempty"`
	StatusCode   int             `json:"status_code"`
	ResponseBody *string         `json:"response_body,omitempty"`
	Success      bool            `json:"success"`
	Attempt      int             `json:"attempt"`
}

func (h *DeliveryHandler) listDeliveries(w http.ResponseWriter, r *http.Request) {
	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))
	webhookID := r.URL.Query().Get("webhook_id")
	eventID := r.URL.Query().Get("event_id")

	var success *bool
	if s := r.URL.Query().Get("success"); s != "" {
		b := s == "true"
		success = &b
	}

	params := store.ListDeliveriesParams{
		Limit:     limit,
		Offset:    offset,
		WebhookID: webhookID,
		EventID:   eventID,
		Success:   success,
	}

	deliveries, total, err := h.store.ListDeliveries(r.Context(), params)
	if err != nil {
		slog.Error("failed to list deliveries", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to list deliveries")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"data":  deliveries,
		"total": total,
	})
}

func (h *DeliveryHandler) createDelivery(w http.ResponseWriter, r *http.Request) {
	var req createDeliveryRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.WebhookID == "" || req.EventID == "" || req.EventType == "" {
		writeError(w, http.StatusBadRequest, "webhook_id, event_id, and event_type are required")
		return
	}

	var payload *string
	if len(req.Payload) > 0 {
		s := string(req.Payload)
		payload = &s
	}

	delivery, err := h.store.CreateDelivery(r.Context(), store.CreateDeliveryParams{
		WebhookID:    req.WebhookID,
		EventID:      req.EventID,
		EventType:    req.EventType,
		Payload:      payload,
		StatusCode:   req.StatusCode,
		ResponseBody: req.ResponseBody,
		Success:      req.Success,
		Attempt:      req.Attempt,
	})
	if err != nil {
		slog.Error("failed to create delivery", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to create delivery")
		return
	}

	writeJSON(w, http.StatusCreated, delivery)
}
