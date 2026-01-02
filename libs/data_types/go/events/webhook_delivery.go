package events

import "time"

// Record of a webhook delivery attempt.
type WebhookDelivery struct {
	ID string `json:"id"`
	WebhookID string `json:"webhook_id"`
	EventID string `json:"event_id"`
	EventType string `json:"event_type"`
	Payload *string `json:"payload,omitempty"`
	StatusCode int `json:"status_code"`
	ResponseBody *string `json:"response_body,omitempty"`
	Success bool `json:"success"`
	Attempt int `json:"attempt"`
	CreatedAt time.Time `json:"created_at"`
	Webhook *Webhook `json:"webhook,omitempty"`
}
