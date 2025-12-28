package auth

import "time"

// LinkedAccount OAuth provider account linked to a user.
Stores provider-specific identity info. Token storage is optional
(only needed if we want to call provider APIs on behalf of user).
type LinkedAccount struct {
	Id string `json:"id"`
	UserId string `json:"user_id"`
	Provider string `json:"provider"`
	ProviderUserId string `json:"provider_user_id"`
	Email *string `json:"email,omitempty"`
	Username *string `json:"username,omitempty"`
	AvatarUrl *string `json:"avatar_url,omitempty"`
	AccessToken *string `json:"access_token,omitempty"`
	RefreshToken *string `json:"refresh_token,omitempty"`
	TokenExpiresAt *time.Time `json:"token_expires_at,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}
