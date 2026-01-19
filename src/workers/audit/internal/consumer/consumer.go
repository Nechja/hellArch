package consumer

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"strings"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/golibs/events"
	"github.com/nechja/hellarch/golibs/rabbitmq"
)

// sensitiveFields lists field names that should be redacted from audit logs
// to prevent accidental storage of credentials, tokens, or PII
var sensitiveFields = map[string]bool{
	"password":        true,
	"token":           true,
	"secret":          true,
	"api_key":         true,
	"apikey":          true,
	"access_token":    true,
	"refresh_token":   true,
	"session_token":   true,
	"authorization":   true,
	"credential":      true,
	"private_key":     true,
	"credit_card":     true,
	"card_number":     true,
	"cvv":             true,
	"ssn":             true,
	"social_security": true,
}

type Consumer struct {
	consumer     *rabbitmq.Consumer
	eventsClient *events.Client
}

func New(ctx context.Context, rabbitURL string, eventsClient *events.Client) (*Consumer, error) {
	consumer, err := rabbitmq.NewConsumer(ctx, rabbitmq.ConsumerConfig{
		URL:        rabbitURL,
		QueueName:  "audit-worker",
		RoutingKey: "hellarch.#",
		TracerName: "audit-worker",
	})
	if err != nil {
		return nil, err
	}

	return &Consumer{
		consumer:     consumer,
		eventsClient: eventsClient,
	}, nil
}

func (c *Consumer) Close() error {
	return c.consumer.Close()
}

func (c *Consumer) IsHealthy() bool {
	return c.consumer.IsHealthy()
}

func (c *Consumer) Run(ctx context.Context) error {
	return c.consumer.Run(ctx, c.processEvent)
}

func (c *Consumer) processEvent(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error {
	actorID := extractActorID(event.Data)
	targetID := extractTargetID(event.Data)

	// Redact sensitive fields before storing in audit log
	redactedData := redactSensitiveFields(event.Data)

	req := events.CreateAuditLogRequest{
		EventType: event.Type,
		EventID:   event.ID,
		Source:    event.Source,
		ActorID:   actorID,
		TargetID:  targetID,
		Metadata:  redactedData,
	}

	if err := c.eventsClient.CreateAuditLog(ctx, req); err != nil {
		return fmt.Errorf("failed to create audit log: %w", err)
	}

	slog.Debug("audit log created", "event_type", event.Type, "event_id", event.ID)
	return nil
}

func extractActorID(data json.RawMessage) *string {
	var m map[string]any
	if err := json.Unmarshal(data, &m); err != nil {
		return nil
	}
	if v, ok := m["user_id"].(string); ok {
		return &v
	}
	return nil
}

func extractTargetID(data json.RawMessage) *string {
	var m map[string]any
	if err := json.Unmarshal(data, &m); err != nil {
		return nil
	}
	if v, ok := m["session_id"].(string); ok {
		return &v
	}
	if v, ok := m["target_id"].(string); ok {
		return &v
	}
	return nil
}

// redactSensitiveFields recursively redacts sensitive field values in JSON data
func redactSensitiveFields(data json.RawMessage) json.RawMessage {
	var m map[string]any
	if err := json.Unmarshal(data, &m); err != nil {
		// If not a JSON object, return as-is
		return data
	}

	redacted := redactMap(m)
	result, err := json.Marshal(redacted)
	if err != nil {
		return data
	}
	return result
}

func redactMap(m map[string]any) map[string]any {
	result := make(map[string]any)
	for key, value := range m {
		lowerKey := strings.ToLower(key)
		if isSensitiveField(lowerKey) {
			result[key] = "[REDACTED]"
		} else {
			result[key] = redactValue(value)
		}
	}
	return result
}

func redactValue(v any) any {
	switch val := v.(type) {
	case map[string]any:
		return redactMap(val)
	case []any:
		result := make([]any, len(val))
		for i, item := range val {
			result[i] = redactValue(item)
		}
		return result
	default:
		return v
	}
}

func isSensitiveField(field string) bool {
	if sensitiveFields[field] {
		return true
	}
	// Also check for partial matches (e.g., "user_password", "auth_token")
	for sensitive := range sensitiveFields {
		if strings.Contains(field, sensitive) {
			return true
		}
	}
	return false
}
