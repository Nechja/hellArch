package ops

import "time"

// Published when a backup completes successfully.
// Event type: hellarch.ops.backup.completed.v1
type BackupCompleted struct {
	BackupID    string    `json:"backup_id"`
	Service     string    `json:"service"`
	BackupType  string    `json:"backup_type"` // "full", "incremental", "snapshot"
	SizeBytes   int64     `json:"size_bytes"`
	Duration    int       `json:"duration_seconds"`
	CompletedAt time.Time `json:"completed_at"`
}

// Published when a backup fails.
// Event type: hellarch.ops.backup.failed.v1
type BackupFailed struct {
	BackupID  string    `json:"backup_id"`
	Service   string    `json:"service"`
	Error     string    `json:"error"`
	FailedAt  time.Time `json:"failed_at"`
}
