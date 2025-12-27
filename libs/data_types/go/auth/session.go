import "time"

// User session for cookie-based auth
type Session struct {
	CreatedAt time.Time `json:"created_at"`
	ExpiresAt time.Time `json:"expires_at"`
	ID        string    `json:"id"`
	Token     string    `json:"token"`
	UserID    string    `json:"user_id"`
}
