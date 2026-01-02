package client

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"time"

	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type EventsClient struct {
	baseURL string
	client  *http.Client
}

func NewEventsClient(baseURL string) *EventsClient {
	return &EventsClient{
		baseURL: baseURL,
		client: &http.Client{
			Transport: otelhttp.NewTransport(http.DefaultTransport),
			Timeout:   10 * time.Second,
		},
	}
}

type Webhook struct {
	ID         string   `json:"id"`
	Name       string   `json:"name"`
	URL        string   `json:"url"`
	Secret     string   `json:"secret"`
	EventTypes []string `json:"event_types"`
	IsActive   bool     `json:"is_active"`
}

type WebhooksResponse struct {
	Data []*Webhook `json:"data"`
}

func (c *EventsClient) GetWebhooksForEventType(ctx context.Context, eventType string) ([]*Webhook, error) {
	reqURL := c.baseURL + "/api/webhooks/by-event/" + url.PathEscape(eventType)
	httpReq, err := http.NewRequestWithContext(ctx, http.MethodGet, reqURL, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}

	resp, err := c.client.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	var result WebhooksResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response: %w", err)
	}

	return result.Data, nil
}

type CreateDeliveryRequest struct {
	WebhookID    string          `json:"webhook_id"`
	EventID      string          `json:"event_id"`
	EventType    string          `json:"event_type"`
	Payload      json.RawMessage `json:"payload,omitempty"`
	StatusCode   int             `json:"status_code"`
	ResponseBody *string         `json:"response_body,omitempty"`
	Success      bool            `json:"success"`
	Attempt      int             `json:"attempt"`
}

func (c *EventsClient) CreateDelivery(ctx context.Context, req CreateDeliveryRequest) error {
	body, err := json.Marshal(req)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, c.baseURL+"/api/webhook-deliveries", bytes.NewReader(body))
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}
	httpReq.Header.Set("Content-Type", "application/json")

	resp, err := c.client.Do(httpReq)
	if err != nil {
		return fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated {
		return fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	return nil
}
