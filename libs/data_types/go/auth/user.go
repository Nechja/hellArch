package auth

import "time"

// Core user identity in the auth system.
type User struct {
	ID string `json:"id"`
	Email string `json:"email"`
	Username string `json:"username"`
	IsActive bool `json:"is_active"`
	Permissions *Dictionary<string, bool> `json:"permissions,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	Sessions []*Session `json:"sessions"`
	LinkedAccounts []*LinkedAccount `json:"linked_accounts"`
}
