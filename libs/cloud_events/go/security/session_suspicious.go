package security

import "time"

// Published when a session is created from an unusual location or device.
// Event type: hellarch.security.session.suspicious.v1
type SessionSuspicious struct {
	SessionID  string    `json:"session_id"`
	UserID     string    `json:"user_id"`
	Email      string    `json:"email"`
	IPAddress  string    `json:"ip_address"`
	UserAgent  string    `json:"user_agent"`
	Location   string    `json:"location,omitempty"` // geo location if available
	Reason     string    `json:"reason"`             // "new_device", "new_location", "vpn_detected"
	DetectedAt time.Time `json:"detected_at"`
}
