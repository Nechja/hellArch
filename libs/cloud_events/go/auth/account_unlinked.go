package auth

import "time"

// Published when an OAuth account is unlinked from a user.
// Event type: hellarch.auth.account.unlinked.v1
type AccountUnlinked struct {
	UserID string `json:"user_id"`
	Provider string `json:"provider"`
	UnlinkedAt time.Time `json:"unlinked_at"`
}
