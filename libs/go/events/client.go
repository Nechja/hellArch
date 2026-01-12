package events

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"time"

	"github.com/nechja/hellarch/golibs/httpclient"
)

type Client struct {
	baseURL string
	client  *http.Client
}

func NewClient(baseURL string) *Client {
	return &Client{
		baseURL: baseURL,
		client:  httpclient.NewDefault(),
	}
}

func NewClientWithTimeout(baseURL string, timeout time.Duration) *Client {
	return &Client{
		baseURL: baseURL,
		client:  httpclient.New(timeout),
	}
}

type WebhookFormat string

const (
	WebhookFormatRaw     WebhookFormat = "raw"
	WebhookFormatDiscord WebhookFormat = "discord"
	WebhookFormatSlack   WebhookFormat = "slack"
)

type Webhook struct {
	ID         string        `json:"id"`
	Name       string        `json:"name"`
	URL        string        `json:"url"`
	Secret     string        `json:"secret"`
	EventTypes []string      `json:"event_types"`
	Format     WebhookFormat `json:"format"`
	IsActive   bool          `json:"is_active"`
}

type WebhooksResponse struct {
	Data []*Webhook `json:"data"`
}

type CreateAuditLogRequest struct {
	EventType string          `json:"event_type"`
	EventID   string          `json:"event_id"`
	Source    string          `json:"source"`
	ActorID   *string         `json:"actor_id,omitempty"`
	TargetID  *string         `json:"target_id,omitempty"`
	Metadata  json.RawMessage `json:"metadata,omitempty"`
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

func (c *Client) CreateAuditLog(ctx context.Context, req CreateAuditLogRequest) error {
	return c.postJSON(ctx, "/audit-logs", req, http.StatusCreated)
}

func (c *Client) GetWebhooksForEventType(ctx context.Context, eventType string) ([]*Webhook, error) {
	reqURL := c.baseURL + "/webhooks/by-event/" + url.PathEscape(eventType)
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

func (c *Client) CreateDelivery(ctx context.Context, req CreateDeliveryRequest) error {
	return c.postJSON(ctx, "/webhook-deliveries", req, http.StatusCreated)
}

func (c *Client) postJSON(ctx context.Context, path string, body any, expectedStatus int) error {
	data, err := json.Marshal(body)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, c.baseURL+path, bytes.NewReader(data))
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}
	httpReq.Header.Set("Content-Type", "application/json")

	resp, err := c.client.Do(httpReq)
	if err != nil {
		return fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != expectedStatus {
		return fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	return nil
}
