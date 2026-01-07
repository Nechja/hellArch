package user

import "time"

type Theme string

const (
	ThemeSystem Theme = "system"
	ThemeLight Theme = "light"
	ThemeDark Theme = "dark"
)

// User preferences and settings.
type UserPreferences struct {
	ID string `json:"id"`
	UserProfileID string `json:"user_profile_id"`
	Theme Theme `json:"theme"`
	Locale *string `json:"locale,omitempty"`
	Timezone *string `json:"timezone,omitempty"`
	EmailNotifications bool `json:"email_notifications"`
	PushNotifications bool `json:"push_notifications"`
	InAppNotifications bool `json:"in_app_notifications"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}
