package identity

import "time"

// OAuth 2.0 access token (short-lived, 1 hour).
// Stored as SHA-256 hash for security.
type AccessToken struct {
	TokenHash string `json:"token_hash"`
	UserID string `json:"user_id"`
	ClientID string `json:"client_id"`
	Scope string `json:"scope"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	User *IdentityUser `json:"user,omitempty"`
}
