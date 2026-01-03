package infra

import "time"

// Published when a connection to a critical service fails.
// Event types:
//   - hellarch.infra.database.connection.failed.v1
//   - hellarch.infra.queue.connection.failed.v1
//   - hellarch.infra.cache.connection.failed.v1
type ConnectionFailed struct {
	Service             string    `json:"service"` // "database", "queue", "cache"
	Target              string    `json:"target"`  // connection target (sanitized, no credentials)
	Error               string    `json:"error"`
	ConsecutiveFailures int       `json:"consecutive_failures"`
	DetectedAt          time.Time `json:"detected_at"`
}

// Published when a connection to a critical service is restored.
// Event types:
//   - hellarch.infra.database.connection.recovered.v1
//   - hellarch.infra.queue.connection.recovered.v1
//   - hellarch.infra.cache.connection.recovered.v1
type ConnectionRecovered struct {
	Service         string    `json:"service"` // "database", "queue", "cache"
	Target          string    `json:"target"`
	RecoveredAt     time.Time `json:"recovered_at"`
	DowntimeSeconds int       `json:"downtime_seconds"`
}
