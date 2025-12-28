package auth

import "time"

// User session for cookie-based authentication.
type Session struct {
	ID string `json:"id"`
	UserID string `json:"user_id"`
	Token string `json:"token"`
	IPAddress *string `json:"ip_address,omitempty"`
	UserAgent *string `json:"user_agent,omitempty"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
	User *User `json:"user,omitempty"`
}
