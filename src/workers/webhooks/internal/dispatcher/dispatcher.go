package dispatcher

import (
	"bytes"
	"context"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"time"

	"github.com/nechja/hellarch/workers/webhooks/internal/client"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	"go.opentelemetry.io/otel/trace"
)

var tracer = otel.Tracer("webhooks-dispatcher")

type Dispatcher struct {
	httpClient   *http.Client
	eventsClient *client.EventsClient
	timeout      time.Duration
}

func New(eventsClient *client.EventsClient, timeout time.Duration) *Dispatcher {
	return &Dispatcher{
		httpClient: &http.Client{
			Transport: otelhttp.NewTransport(http.DefaultTransport),
			Timeout:   timeout,
		},
		eventsClient: eventsClient,
		timeout:      timeout,
	}
}

type DeliveryRequest struct {
	Webhook   *client.Webhook
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
			attribute.String("webhook.format", string(req.Webhook.Format)),
			attribute.String("event.id", req.EventID),
			attribute.String("event.type", req.EventType),
			attribute.Int("attempt", req.Attempt),
		),
	)
	defer span.End()

	// Transform payload based on webhook format
	payload, contentType, err := TransformPayload(req.Payload, req.Webhook.Format)
	if err != nil {
		slog.Error("failed to transform payload", "error", err, "format", req.Webhook.Format)
		payload = req.Payload
		contentType = "application/cloudevents+json"
	}

	signature := computeSignature(payload, req.Webhook.Secret)

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, req.Webhook.URL, bytes.NewReader(payload))
	if err != nil {
		return d.recordDelivery(ctx, req, 0, "", false)
	}

	httpReq.Header.Set("Content-Type", contentType)
	// Only set custom headers for raw format (Discord/Slack don't need them)
	if req.Webhook.Format == client.WebhookFormatRaw {
		httpReq.Header.Set("X-Webhook-Signature", signature)
		httpReq.Header.Set("X-Event-ID", req.EventID)
		httpReq.Header.Set("X-Event-Type", req.EventType)
	}

	resp, err := d.httpClient.Do(httpReq)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "request failed")
		return d.recordDelivery(ctx, req, 0, "", false)
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(io.LimitReader(resp.Body, 1024))
	responseBody := string(body)

	success := resp.StatusCode >= 200 && resp.StatusCode < 300
	if success {
		slog.Debug("webhook delivered successfully",
			"webhook_id", req.Webhook.ID,
			"event_id", req.EventID,
			"status_code", resp.StatusCode,
		)
	} else {
		span.RecordError(fmt.Errorf("webhook returned status %d", resp.StatusCode))
		span.SetStatus(codes.Error, "non-2xx response")
	}

	return d.recordDelivery(ctx, req, resp.StatusCode, responseBody, success)
}

func (d *Dispatcher) recordDelivery(ctx context.Context, req DeliveryRequest, statusCode int, responseBody string, success bool) error {
	var respBody *string
	if responseBody != "" {
		respBody = &responseBody
	}

	deliveryReq := client.CreateDeliveryRequest{
		WebhookID:    req.Webhook.ID,
		EventID:      req.EventID,
		EventType:    req.EventType,
		Payload:      json.RawMessage(req.Payload),
		StatusCode:   statusCode,
		ResponseBody: respBody,
		Success:      success,
		Attempt:      req.Attempt,
	}

	if err := d.eventsClient.CreateDelivery(ctx, deliveryReq); err != nil {
		slog.Error("failed to record delivery", "error", err)
	}

	if !success {
		return fmt.Errorf("delivery failed with status %d", statusCode)
	}
	return nil
}

func computeSignature(payload []byte, secret string) string {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(payload)
	return "sha256=" + hex.EncodeToString(mac.Sum(nil))
}
