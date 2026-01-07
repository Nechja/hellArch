package identity

import "time"

// OAuth 2.0 client application registration.
type OAuthClient struct {
	ID string `json:"id"`
	SecretHash string `json:"secret_hash"`
	Name string `json:"name"`
	RedirectUris []string `json:"redirect_uris"`
	IsActive bool `json:"is_active"`
	CreatedAt time.Time `json:"created_at"`
}
