package security

import "time"

// Published when an API token is revoked.
// Event type: hellarch.security.token.revoked.v1
type TokenRevoked struct {
	TokenID   string    `json:"token_id"`
	UserID    string    `json:"user_id"`
	TokenName string    `json:"token_name,omitempty"`
	RevokedBy string    `json:"revoked_by"` // user_id or "system"
	Reason    string    `json:"reason"`     // "user_request", "security_concern", "expired", "admin_action"
	RevokedAt time.Time `json:"revoked_at"`
}
