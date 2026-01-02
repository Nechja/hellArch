package dispatcher

import (
	"bytes"
	"context"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"time"

	"github.com/nechja/hellarch/workers/webhooks/internal/store"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	"go.opentelemetry.io/otel/trace"
)

var tracer = otel.Tracer("webhooks-dispatcher")

type Dispatcher struct {
	client  *http.Client
	store   *store.Store
	timeout time.Duration
}

func New(store *store.Store, timeout time.Duration) *Dispatcher {
	return &Dispatcher{
		client: &http.Client{
			Timeout: timeout,
		},
		store:   store,
		timeout: timeout,
	}
}

type DeliveryRequest struct {
	Webhook   store.Webhook
	EventID   string
	EventType string
	Payload   []byte
	Attempt   int
}

func (d *Dispatcher) Deliver(ctx context.Context, req DeliveryRequest) error {
	ctx, span := tracer.Start(ctx, "webhook.deliver",
		trace.WithAttributes(
			attribute.String("webhook.id", req.Webhook.ID),
			attribute.String("webhook.url", req.Webhook.URL),
			attribute.String("event.id", req.EventID),
			attribute.String("event.type", req.EventType),
			attribute.Int("attempt", req.Attempt),
		),
	)
	defer span.End()

	signature := computeSignature(req.Payload, req.Webhook.Secret)

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, req.Webhook.URL, bytes.NewReader(req.Payload))
	if err != nil {
		return d.recordDelivery(ctx, req, nil, "", fmt.Errorf("failed to create request: %w", err))
	}

	httpReq.Header.Set("Content-Type", "application/cloudevents+json")
	httpReq.Header.Set("X-Webhook-Signature", signature)
	httpReq.Header.Set("X-Event-ID", req.EventID)
	httpReq.Header.Set("X-Event-Type", req.EventType)

	resp, err := d.client.Do(httpReq)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "request failed")
		return d.recordDelivery(ctx, req, nil, "", fmt.Errorf("request failed: %w", err))
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(io.LimitReader(resp.Body, 1024))
	responseBody := string(body)

	if resp.StatusCode >= 200 && resp.StatusCode < 300 {
		slog.Debug("webhook delivered successfully",
			"webhook_id", req.Webhook.ID,
			"event_id", req.EventID,
			"status_code", resp.StatusCode,
		)
		return d.recordDelivery(ctx, req, &resp.StatusCode, responseBody, nil)
	}

	err = fmt.Errorf("webhook returned status %d", resp.StatusCode)
	span.RecordError(err)
	span.SetStatus(codes.Error, "non-2xx response")
	return d.recordDelivery(ctx, req, &resp.StatusCode, responseBody, err)
}

func (d *Dispatcher) recordDelivery(ctx context.Context, req DeliveryRequest, statusCode *int, responseBody string, deliveryErr error) error {
	var errStr *string
	if deliveryErr != nil {
		s := deliveryErr.Error()
		errStr = &s
	}

	var respBody *string
	if responseBody != "" {
		respBody = &responseBody
	}

	delivery := &store.WebhookDelivery{
		WebhookID:     req.Webhook.ID,
		EventID:       req.EventID,
		EventType:     req.EventType,
		Payload:       req.Payload,
		StatusCode:    statusCode,
		ResponseBody:  respBody,
		Error:         errStr,
		AttemptNumber: req.Attempt,
		CreatedAt:     time.Now().UTC(),
	}

	if err := d.store.InsertDelivery(ctx, delivery); err != nil {
		slog.Error("failed to record delivery", "error", err)
	}

	return deliveryErr
}

func computeSignature(payload []byte, secret string) string {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(payload)
	return "sha256=" + hex.EncodeToString(mac.Sum(nil))
}
