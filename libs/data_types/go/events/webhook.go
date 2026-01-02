package events

import "time"

type WebhookFormat string

const (
	WebhookFormatRaw WebhookFormat = "raw"
	WebhookFormatDiscord WebhookFormat = "discord"
	WebhookFormatSlack WebhookFormat = "slack"
)

// Webhook endpoint registration for event delivery.
type Webhook struct {
	ID string `json:"id"`
	Name string `json:"name"`
	URL string `json:"url"`
	Secret string `json:"secret"`
	EventTypes []*string `json:"event_types"`
	Format WebhookFormat `json:"format"`
	IsActive bool `json:"is_active"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	Deliveries []*WebhookDelivery `json:"deliveries"`
}
