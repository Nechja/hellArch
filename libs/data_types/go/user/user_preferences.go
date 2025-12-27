import "time"

// User preferences and settings
type UserPreferences struct {
	CreatedAt          time.Time `json:"created_at"`
	EmailNotifications bool      `json:"email_notifications"`
	ID                 string    `json:"id"`
	Locale             *string   `json:"locale"`
	Theme              Theme     `json:"theme"`
	Timezone           *string   `json:"timezone"`
	UpdatedAt          time.Time `json:"updated_at"`
	UserProfileID      string    `json:"user_profile_id"`
}

type Theme string

const (
	Dark   Theme = "dark"
	Light  Theme = "light"
	System Theme = "system"
)
