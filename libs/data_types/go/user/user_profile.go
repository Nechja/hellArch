package user

import "time"

// Extended user profile information.
// Linked to auth user via AuthUserId.
// Email is denormalized from auth for read performance.
type UserProfile struct {
	ID string `json:"id"`
	AuthUserID string `json:"auth_user_id"`
	Email string `json:"email"`
	DisplayName *string `json:"display_name,omitempty"`
	Bio *string `json:"bio,omitempty"`
	AvatarURL *string `json:"avatar_url,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	Preferences *UserPreferences `json:"preferences,omitempty"`
}
