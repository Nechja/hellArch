package events

import "time"

// Audit log entry for tracking system events.
type AuditLog struct {
	ID string `json:"id"`
	EventType string `json:"event_type"`
	EventID string `json:"event_id"`
	Source string `json:"source"`
	ActorID *string `json:"actor_id,omitempty"`
	TargetID *string `json:"target_id,omitempty"`
	Metadata *string `json:"metadata,omitempty"`
	CreatedAt time.Time `json:"created_at"`
}
