package infra

import "time"

// Published when a service becomes unhealthy.
// Event type: hellarch.infra.service.unhealthy.v1
type ServiceUnhealthy struct {
	ServiceName string `json:"service_name"`
	Status string `json:"status"`
	ConsecutiveFailures int32 `json:"consecutive_failures"`
	LastError *string `json:"last_error,omitempty"`
	DetectedAt time.Time `json:"detected_at"`
}
