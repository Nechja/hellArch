package auth

import "time"

// Published when an OAuth account is linked to a user.
// Event type: hellarch.auth.account.linked.v1
type AccountLinked struct {
	UserID string `json:"user_id"`
	Provider string `json:"provider"`
	ProviderUserID string `json:"provider_user_id"`
	Email string `json:"email"`
	LinkedAt time.Time `json:"linked_at"`
}
