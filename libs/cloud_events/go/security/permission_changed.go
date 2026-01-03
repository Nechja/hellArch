package security

import "time"

// Published when a user's permissions are modified.
// Event type: hellarch.security.permission.changed.v1
type PermissionChanged struct {
	UserID    string    `json:"user_id"`
	Email     string    `json:"email"`
	ChangedBy string    `json:"changed_by"`
	OldRole   string    `json:"old_role"`
	NewRole   string    `json:"new_role"`
	ChangedAt time.Time `json:"changed_at"`
}
