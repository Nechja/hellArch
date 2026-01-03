package security

import "time"

// Published when suspicious login activity is detected.
// Event type: hellarch.security.login.suspicious.v1
type LoginSuspicious struct {
	UserID            string    `json:"user_id,omitempty"`
	Email             string    `json:"email"`
	IPAddress         string    `json:"ip_address"`
	FailedAttempts    int       `json:"failed_attempts"`
	TimeWindowMinutes int       `json:"time_window_minutes"`
	Reason            string    `json:"reason"` // "brute_force", "credential_stuffing", "new_location"
	DetectedAt        time.Time `json:"detected_at"`
}
