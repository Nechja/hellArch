package auth

import "time"

type SessionEndReason string

const (
	SessionEndReasonLogout SessionEndReason = "logout"
	SessionEndReasonExpired SessionEndReason = "expired"
	SessionEndReasonRevoked SessionEndReason = "revoked"
)

// Published when a new session is created.
// Event type: hellarch.auth.session.created.v1
type SessionCreated struct {
	SessionID string `json:"session_id"`
	UserID string `json:"user_id"`
	IPAddress string `json:"ip_address"`
	UserAgent string `json:"user_agent"`
	CreatedAt time.Time `json:"created_at"`
	ExpiresAt time.Time `json:"expires_at"`
}
