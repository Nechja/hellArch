package identity

import "time"

// Local identity user with username/password authentication.
type IdentityUser struct {
	ID string `json:"id"`
	Email string `json:"email"`
	Username string `json:"username"`
	PasswordHash string `json:"password_hash"`
	IsActive bool `json:"is_active"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	RefreshTokens []*RefreshToken `json:"refresh_tokens"`
}
