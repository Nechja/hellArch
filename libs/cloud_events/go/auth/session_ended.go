package auth

import "time"

type SessionEndReason string

const (
	SessionEndReasonLogout SessionEndReason = "logout"
	SessionEndReasonExpired SessionEndReason = "expired"
	SessionEndReasonRevoked SessionEndReason = "revoked"
)

// Published when a session ends (logout or expiry).
// Event type: hellarch.auth.session.ended.v1
type SessionEnded struct {
	SessionID string `json:"session_id"`
	UserID string `json:"user_id"`
	Reason SessionEndReason `json:"reason"`
	EndedAt time.Time `json:"ended_at"`
}
