package ops

import "time"

// Published when a webhook has consecutive delivery failures.
// Event type: hellarch.ops.webhook.failing.v1
type WebhookFailing struct {
	WebhookID        string    `json:"webhook_id"`
	WebhookName      string    `json:"webhook_name"`
	WebhookURL       string    `json:"webhook_url"` // may be partially redacted
	ConsecutiveFails int       `json:"consecutive_failures"`
	LastError        string    `json:"last_error"`
	LastStatusCode   int       `json:"last_status_code"`
	DetectedAt       time.Time `json:"detected_at"`
}
