package ops

import "time"

// Published when a rate limit is triggered.
// Event type: hellarch.ops.ratelimit.exceeded.v1
type RateLimitExceeded struct {
	UserID      string    `json:"user_id,omitempty"`
	IPAddress   string    `json:"ip_address"`
	Endpoint    string    `json:"endpoint"`
	Limit       int       `json:"limit"`
	Window      string    `json:"window"` // "1m", "1h", "24h"
	TriggeredAt time.Time `json:"triggered_at"`
}
