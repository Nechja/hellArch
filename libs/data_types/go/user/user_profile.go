package user

import "time"

// UserProfile Extended user profile information.
Linked to auth user via AuthUserId.
Email is denormalized from auth for read performance.
type UserProfile struct {
	Id string `json:"id"`
	AuthUserId string `json:"auth_user_id"`
	Email string `json:"email"`
	DisplayName *string `json:"display_name,omitempty"`
	Bio *string `json:"bio,omitempty"`
	AvatarUrl *string `json:"avatar_url,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	Preferences *UserPreferences `json:"preferences,omitempty"`
}
