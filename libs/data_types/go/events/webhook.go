package events

import "time"

// A registered webhook endpoint that receives event notifications.
type Webhook struct {
	ID string `json:"id"`
	URL string `json:"url"`
	Secret string `json:"secret"`
	EventTypes []*string `json:"event_types"`
	IsActive bool `json:"is_active"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	Deliveries []*WebhookDelivery `json:"deliveries"`
}
