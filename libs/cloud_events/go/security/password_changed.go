package security

import "time"

// Published when a user changes their password.
// Event type: hellarch.security.password.changed.v1
type PasswordChanged struct {
	UserID    string    `json:"user_id"`
	Email     string    `json:"email"`
	Method    string    `json:"method"` // "user_initiated", "admin_reset", "forgot_password"
	IPAddress string    `json:"ip_address,omitempty"`
	ChangedAt time.Time `json:"changed_at"`
}
