import "time"

// Core user identity
type User struct {
	CreatedAt time.Time `json:"created_at"`
	Email     string    `json:"email"`
	ID        string    `json:"id"`
	UpdatedAt time.Time `json:"updated_at"`
	Username  string    `json:"username"`
}
