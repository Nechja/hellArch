package identity

import "time"

// OAuth 2.0 refresh token (long-lived, 30 days).
type RefreshToken struct {
	Token string `json:"token"`
	UserID string `json:"user_id"`
	ClientID string `json:"client_id"`
	Scope string `json:"scope"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	User *IdentityUser `json:"user,omitempty"`
}
