package events

import "time"

// Stores a record of every event that passes through the system for audit purposes.
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
