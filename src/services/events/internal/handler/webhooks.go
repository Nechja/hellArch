package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/services/events/internal/store"
)

type createWebhookRequest struct {
	Name       string `json:"name"`
	URL        string `json:"url"`
	Secret     string `json:"secret"`
	EventTypes []string `json:"event_types"`
	Format     string `json:"format"` // raw, discord, slack
}

type updateWebhookRequest struct {
	Name       *string  `json:"name,omitempty"`
	URL        *string  `json:"url,omitempty"`
	Secret     *string  `json:"secret,omitempty"`
	EventTypes []string `json:"event_types,omitempty"`
	Format     *string  `json:"format,omitempty"`
	IsActive   *bool    `json:"is_active,omitempty"`
}

func (h *Handler) listWebhooks(w http.ResponseWriter, r *http.Request) {
	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))

	webhooks, total, err := h.store.ListWebhooks(r.Context(), limit, offset)
	if err != nil {
		slog.Error("failed to list webhooks", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to list webhooks")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"data":  webhooks,
		"total": total,
	})
}

func (h *Handler) createWebhook(w http.ResponseWriter, r *http.Request) {
	var req createWebhookRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.Name == "" || req.URL == "" || req.Secret == "" {
		writeError(w, http.StatusBadRequest, "name, url, and secret are required")
		return
	}

	// Validate format
	format := store.WebhookFormat(req.Format)
	if format == "" {
		format = store.WebhookFormatRaw
	}
	if format != store.WebhookFormatRaw && format != store.WebhookFormatDiscord && format != store.WebhookFormatSlack {
		writeError(w, http.StatusBadRequest, "format must be one of: raw, discord, slack")
		return
	}

	webhook, err := h.store.CreateWebhook(r.Context(), store.CreateWebhookParams{
		Name:       req.Name,
		URL:        req.URL,
		Secret:     req.Secret,
		EventTypes: req.EventTypes,
		Format:     format,
	})
	if err != nil {
		slog.Error("failed to create webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to create webhook")
		return
	}

	writeJSON(w, http.StatusCreated, webhook)
}

func (h *Handler) getWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	webhook, err := h.store.GetWebhook(r.Context(), id)
	if err != nil {
		slog.Error("failed to get webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to get webhook")
		return
	}
	if webhook == nil {
		writeError(w, http.StatusNotFound, "Webhook not found")
		return
	}

	writeJSON(w, http.StatusOK, webhook)
}

func (h *Handler) updateWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	var req updateWebhookRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	// Validate format if provided
	var format *store.WebhookFormat
	if req.Format != nil {
		f := store.WebhookFormat(*req.Format)
		if f != store.WebhookFormatRaw && f != store.WebhookFormatDiscord && f != store.WebhookFormatSlack {
			writeError(w, http.StatusBadRequest, "format must be one of: raw, discord, slack")
			return
		}
		format = &f
	}

	webhook, err := h.store.UpdateWebhook(r.Context(), id, store.UpdateWebhookParams{
		Name:       req.Name,
		URL:        req.URL,
		Secret:     req.Secret,
		EventTypes: req.EventTypes,
		Format:     format,
		IsActive:   req.IsActive,
	})
	if err != nil {
		slog.Error("failed to update webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to update webhook")
		return
	}
	if webhook == nil {
		writeError(w, http.StatusNotFound, "Webhook not found")
		return
	}

	writeJSON(w, http.StatusOK, webhook)
}

func (h *Handler) deleteWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	if err := h.store.DeleteWebhook(r.Context(), id); err != nil {
		slog.Error("failed to delete webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to delete webhook")
		return
	}

	w.WriteHeader(http.StatusNoContent)
}

func (h *Handler) getWebhooksByEventType(w http.ResponseWriter, r *http.Request) {
	eventType := r.PathValue("type")

	webhooks, err := h.store.GetWebhooksForEventType(r.Context(), eventType)
	if err != nil {
		slog.Error("failed to get webhooks for event type", "error", err, "event_type", eventType)
		writeError(w, http.StatusInternalServerError, "Failed to get webhooks")
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"data": webhooks,
	})
}
