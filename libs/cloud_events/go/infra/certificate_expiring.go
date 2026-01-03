package infra

import "time"

// Published when an SSL certificate is approaching expiration.
// Event type: hellarch.infra.certificate.expiring.v1
type CertificateExpiring struct {
	Domain     string    `json:"domain"`
	ExpiresAt  time.Time `json:"expires_at"`
	DaysLeft   int       `json:"days_left"`
	Issuer     string    `json:"issuer"`
	DetectedAt time.Time `json:"detected_at"`
}
