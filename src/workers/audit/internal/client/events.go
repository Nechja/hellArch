package client

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"net/http"
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

type CreateAuditLogRequest struct {
	EventType string          `json:"event_type"`
	EventID   string          `json:"event_id"`
	Source    string          `json:"source"`
	ActorID   *string         `json:"actor_id,omitempty"`
	TargetID  *string         `json:"target_id,omitempty"`
	Metadata  json.RawMessage `json:"metadata,omitempty"`
}

func (c *EventsClient) CreateAuditLog(ctx context.Context, req CreateAuditLogRequest) error {
	body, err := json.Marshal(req)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, c.baseURL+"/api/audit-logs", bytes.NewReader(body))
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
