package user

import "time"

// UserProfile Extended user profile information.
Linked to auth user via AuthUserId.
type UserProfile struct {
	Id string `json:"id"`
	AuthUserId string `json:"auth_user_id"`
	DisplayName *string `json:"display_name,omitempty"`
	Bio *string `json:"bio,omitempty"`
	AvatarUrl *string `json:"avatar_url,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}
