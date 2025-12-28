package auth

import "time"

// Session User session for cookie-based authentication.
type Session struct {
	Id string `json:"id"`
	UserId string `json:"user_id"`
	Token string `json:"token"`
	IpAddress *string `json:"ip_address,omitempty"`
	UserAgent *string `json:"user_agent,omitempty"`
	ExpiresAt time.Time `json:"expires_at"`
	CreatedAt time.Time `json:"created_at"`
}
