package auth

import "time"

// User Core user identity in the auth system.
type User struct {
	Id string `json:"id"`
	Email string `json:"email"`
	Username string `json:"username"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}
