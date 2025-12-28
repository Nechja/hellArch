package auth

import "time"

// Published when a new user is created via OAuth.
// Event type: hellarch.auth.user.created.v1
type UserCreated struct {
	UserID string `json:"user_id"`
	Email string `json:"email"`
	Username string `json:"username"`
	Provider string `json:"provider"`
	ProviderUserID string `json:"provider_user_id"`
	AvatarURL string `json:"avatar_url"`
	CreatedAt time.Time `json:"created_at"`
}
