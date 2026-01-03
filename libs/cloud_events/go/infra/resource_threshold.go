package infra

import "time"

// Published when resource usage exceeds a threshold.
// Event type: hellarch.infra.resource.threshold.v1
type ResourceThreshold struct {
	Service    string    `json:"service"`
	Host       string    `json:"host,omitempty"`
	Resource   string    `json:"resource"`  // "cpu", "memory", "disk"
	Current    float64   `json:"current"`   // percentage (0-100)
	Threshold  float64   `json:"threshold"` // percentage (0-100)
	DetectedAt time.Time `json:"detected_at"`
}
