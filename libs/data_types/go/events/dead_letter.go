package events

import "time"

// Dead letter entry for tracking failed events that exhausted retries.
type DeadLetter struct {
	ID string `json:"id"`
	EventType string `json:"event_type"`
	EventID string `json:"event_id"`
	Source string `json:"source"`
	OriginalQueue string `json:"original_queue"`
	DeathReason string `json:"death_reason"`
	RetryCount int `json:"retry_count"`
	Body string `json:"body"`
	ErrorMessage *string `json:"error_message,omitempty"`
	CreatedAt time.Time `json:"created_at"`
}
