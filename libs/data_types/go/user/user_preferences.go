package user

import "time"

type Theme string

const (
	ThemeSystem Theme = "system"
	ThemeLight Theme = "light"
	ThemeDark Theme = "dark"
)

// UserPreferences User preferences and settings.
type UserPreferences struct {
	Id string `json:"id"`
	UserProfileId string `json:"user_profile_id"`
	Theme Theme `json:"theme"`
	Locale *string `json:"locale,omitempty"`
	Timezone *string `json:"timezone,omitempty"`
	EmailNotifications bool `json:"email_notifications"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}
