package identity

import "time"

// OAuth 2.0 authorization code grant (short-lived, 5 minutes).
type AuthorizationCode struct {
	Code string `json:"code"`
	ClientID string `json:"client_id"`
	UserID string `json:"user_id"`
	RedirectUri string `json:"redirect_uri"`
	Scope string `json:"scope"`
	CodeChallenge *string `json:"code_challenge,omitempty"`
	CodeChallengeMethod *string `json:"code_challenge_method,omitempty"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	User *IdentityUser `json:"user,omitempty"`
}
