package auth

import "time"

// Published when a user is deleted.
// Event type: hellarch.auth.user.deleted.v1
type UserDeleted struct {
	UserID string `json:"user_id"`
	DeletedAt time.Time `json:"deleted_at"`
}
