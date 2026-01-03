package ops

import "time"

// DeploymentStatus represents the status of a deployment.
type DeploymentStatus string

const (
	DeploymentStatusStarted    DeploymentStatus = "started"
	DeploymentStatusCompleted  DeploymentStatus = "completed"
	DeploymentStatusFailed     DeploymentStatus = "failed"
	DeploymentStatusRolledback DeploymentStatus = "rolledback"
)

// Published for deployment lifecycle events.
// Event types:
//   - hellarch.ops.deployment.started.v1
//   - hellarch.ops.deployment.completed.v1
//   - hellarch.ops.deployment.failed.v1
//   - hellarch.ops.deployment.rolledback.v1
type Deployment struct {
	DeploymentID string           `json:"deployment_id"`
	Service      string           `json:"service"`
	Version      string           `json:"version"`
	Environment  string           `json:"environment"` // "production", "staging", "development"
	InitiatedBy  string           `json:"initiated_by"`
	Status       DeploymentStatus `json:"status"`
	Error        string           `json:"error,omitempty"`
	Timestamp    time.Time        `json:"timestamp"`
}
