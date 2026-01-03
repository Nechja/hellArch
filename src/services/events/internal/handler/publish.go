package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/google/uuid"
)

func (h *Handler) publishEvent(w http.ResponseWriter, r *http.Request) {
	var event cloudevents.CloudEvent[json.RawMessage]
	if err := json.NewDecoder(r.Body).Decode(&event); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid CloudEvent format")
		return
	}

	if event.Type == "" {
		writeError(w, http.StatusBadRequest, "Event type is required")
		return
	}

	// Auto-generate ID if not provided
	if event.ID == "" {
		event.ID = uuid.New().String()
	}

	if h.publisher == nil {
		writeError(w, http.StatusServiceUnavailable, "Publisher not available")
		return
	}

	if err := h.publisher.PublishRaw(r.Context(), event); err != nil {
		slog.Error("failed to publish event", "error", err, "event_type", event.Type)
		writeError(w, http.StatusInternalServerError, "Failed to publish event")
		return
	}

	slog.Debug("event published", "type", event.Type, "id", event.ID)
	w.WriteHeader(http.StatusAccepted)
}
