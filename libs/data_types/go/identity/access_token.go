package identity

import "time"

// OAuth 2.0 access token (short-lived, 1 hour).
// Stored for validation until expiry.
type AccessToken struct {
	Token string `json:"token"`
	UserID string `json:"user_id"`
	ClientID string `json:"client_id"`
	Scope string `json:"scope"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	User *IdentityUser `json:"user,omitempty"`
}
