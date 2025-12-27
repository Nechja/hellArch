import "time"

// OAuth provider linked to a user
type LinkedAccount struct {
	AccessToken    *string    `json:"access_token"`
	AvatarURL      *string    `json:"avatar_url"`
	CreatedAt      time.Time  `json:"created_at"`
	Email          *string    `json:"email"`
	ExpiresAt      *time.Time `json:"expires_at"`
	ID             string     `json:"id"`
	Provider       string     `json:"provider"`
	ProviderUserID string     `json:"provider_user_id"`
	RefreshToken   *string    `json:"refresh_token"`
	UpdatedAt      time.Time  `json:"updated_at"`
	UserID         string     `json:"user_id"`
	Username       *string    `json:"username"`
}
