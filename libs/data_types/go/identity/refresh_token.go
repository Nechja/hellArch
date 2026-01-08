package identity

import "time"

// OAuth 2.0 refresh token (long-lived, 30 days).
// Stored as SHA-256 hash for security.
// Supports token rotation with reuse detection.
type RefreshToken struct {
	TokenHash string `json:"token_hash"`
	UserID string `json:"user_id"`
	ClientID string `json:"client_id"`
	Scope string `json:"scope"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	FamilyID string `json:"family_id"`
	IsRevoked bool `json:"is_revoked"`
	ClientFingerprint *string `json:"client_fingerprint,omitempty"`
	User *IdentityUser `json:"user,omitempty"`
}
