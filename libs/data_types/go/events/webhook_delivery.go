package events

import "time"

// Tracks each attempt to deliver an event to a webhook endpoint.
type WebhookDelivery struct {
	ID string `json:"id"`
	WebhookID string `json:"webhook_id"`
	EventID string `json:"event_id"`
	EventType string `json:"event_type"`
	Payload string `json:"payload"`
	StatusCode *int `json:"status_code,omitempty"`
	ResponseBody *string `json:"response_body,omitempty"`
	Error *string `json:"error,omitempty"`
	AttemptNumber int `json:"attempt_number"`
	CreatedAt time.Time `json:"created_at"`
	Webhook Webhook `json:"webhook"`
}
