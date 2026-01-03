package consumer

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/workers/audit/internal/client"
)

type Consumer struct {
	consumer     *rabbitmq.Consumer
	eventsClient *client.EventsClient
}

func New(ctx context.Context, rabbitURL string, eventsClient *client.EventsClient) (*Consumer, error) {
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

	req := client.CreateAuditLogRequest{
		EventType: event.Type,
		EventID:   event.ID,
		Source:    event.Source,
		ActorID:   actorID,
		TargetID:  targetID,
		Metadata:  event.Data,
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
