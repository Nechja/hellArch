import "time"

// Extended user profile info
type UserProfile struct {
	AuthUserID  string    `json:"auth_user_id"`
	AvatarURL   *string   `json:"avatar_url"`
	Bio         *string   `json:"bio"`
	CreatedAt   time.Time `json:"created_at"`
	DisplayName *string   `json:"display_name"`
	ID          string    `json:"id"`
	UpdatedAt   time.Time `json:"updated_at"`
}
