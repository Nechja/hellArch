package security

import "time"

// Published when a login attempt fails.
// Event type: hellarch.security.login.failed.v1
type LoginFailed struct {
	Email       string    `json:"email"`
	IPAddress   string    `json:"ip_address"`
	UserAgent   string    `json:"user_agent"`
	Reason      string    `json:"reason"` // "invalid_credentials", "account_locked", "account_disabled"
	AttemptedAt time.Time `json:"attempted_at"`
}
