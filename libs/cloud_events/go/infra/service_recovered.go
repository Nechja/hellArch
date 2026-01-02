package infra

import "time"

// Published when a service recovers from unhealthy state.
// Event type: hellarch.infra.service.recovered.v1
type ServiceRecovered struct {
	ServiceName string `json:"service_name"`
	RecoveredAt time.Time `json:"recovered_at"`
	DowntimeSeconds int32 `json:"downtime_seconds"`
}
