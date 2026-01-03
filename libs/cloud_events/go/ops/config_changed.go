package ops

import "time"

// Published when a configuration value is changed.
// Event type: hellarch.ops.config.changed.v1
type ConfigChanged struct {
	Service   string    `json:"service"`
	ConfigKey string    `json:"config_key"`
	ChangedBy string    `json:"changed_by"`
	OldValue  string    `json:"old_value,omitempty"` // may be redacted for secrets
	NewValue  string    `json:"new_value,omitempty"` // may be redacted for secrets
	ChangedAt time.Time `json:"changed_at"`
}
