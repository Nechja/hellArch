package security

import "time"

// Published when an account is locked due to security concerns.
// Event type: hellarch.security.account.locked.v1
type AccountLocked struct {
	UserID      string     `json:"user_id"`
	Email       string     `json:"email"`
	Reason      string     `json:"reason"` // "failed_attempts", "admin_action", "suspicious_activity"
	LockedAt    time.Time  `json:"locked_at"`
	LockedUntil *time.Time `json:"locked_until,omitempty"` // nil = permanent
}

// Published when an account is unlocked.
// Event type: hellarch.security.account.unlocked.v1
type AccountUnlocked struct {
	UserID     string    `json:"user_id"`
	Email      string    `json:"email"`
	UnlockedBy string    `json:"unlocked_by"` // "admin", "system", "user"
	UnlockedAt time.Time `json:"unlocked_at"`
}
