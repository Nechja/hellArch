package dispatcher

import (
	"context"
	"encoding/json"
	"log/slog"

	"github.com/nechja/hellarch/golibs/events"
)

type DeliveryRecorder struct {
	eventsClient *events.Client
}

func NewDeliveryRecorder(eventsClient *events.Client) *DeliveryRecorder {
	return &DeliveryRecorder{eventsClient: eventsClient}
}

type RecordParams struct {
	WebhookID    string
	EventID      string
	EventType    string
	Payload      []byte
	StatusCode   int
	ResponseBody string
	Success      bool
	Attempt      int
}

func (r *DeliveryRecorder) Record(ctx context.Context, params RecordParams) error {
	var respBody *string
	if params.ResponseBody != "" {
		respBody = &params.ResponseBody
	}

	req := events.CreateDeliveryRequest{
		WebhookID:    params.WebhookID,
		EventID:      params.EventID,
		EventType:    params.EventType,
		Payload:      json.RawMessage(params.Payload),
		StatusCode:   params.StatusCode,
		ResponseBody: respBody,
		Success:      params.Success,
		Attempt:      params.Attempt,
	}

	if err := r.eventsClient.CreateDelivery(ctx, req); err != nil {
		slog.Error("failed to record delivery", "error", err)
		return err
	}

	return nil
}
