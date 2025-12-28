package auth

import "time"

// Published when user info is updated.
// Event type: hellarch.auth.user.updated.v1
type UserUpdated struct {
	UserID string `json:"user_id"`
	Email string `json:"email"`
	Username string `json:"username"`
	UpdatedAt time.Time `json:"updated_at"`
}
