package consumer

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/workers/audit/internal/store"
	amqp "github.com/rabbitmq/amqp091-go"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"go.opentelemetry.io/otel/trace"
)

const (
	ExchangeName = "hellarch.events"
	QueueName    = "audit-worker"
	RoutingKey   = "hellarch.#"
)

var tracer = otel.Tracer("audit-worker")

type Consumer struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	store   *store.Store
}

func New(ctx context.Context, rabbitURL string, store *store.Store) (*Consumer, error) {
	conn, err := amqp.Dial(rabbitURL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	if err := ch.ExchangeDeclare(
		ExchangeName,
		"topic",
		true,
		false,
		false,
		false,
		nil,
	); err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to declare exchange: %w", err)
	}

	if _, err := ch.QueueDeclare(
		QueueName,
		true,
		false,
		false,
		false,
		nil,
	); err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to declare queue: %w", err)
	}

	if err := ch.QueueBind(
		QueueName,
		RoutingKey,
		ExchangeName,
		false,
		nil,
	); err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to bind queue: %w", err)
	}

	slog.Info("connected to RabbitMQ", "queue", QueueName, "routing_key", RoutingKey)

	return &Consumer{
		conn:    conn,
		channel: ch,
		store:   store,
	}, nil
}

func (c *Consumer) Close() error {
	if err := c.channel.Close(); err != nil {
		return err
	}
	return c.conn.Close()
}

func (c *Consumer) Run(ctx context.Context) error {
	deliveries, err := c.channel.Consume(
		QueueName,
		"audit-worker-consumer",
		false,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return fmt.Errorf("failed to start consuming: %w", err)
	}

	slog.Info("starting to consume events")

	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		case delivery, ok := <-deliveries:
			if !ok {
				return fmt.Errorf("delivery channel closed")
			}
			c.handleDelivery(ctx, delivery)
		}
	}
}

func (c *Consumer) handleDelivery(ctx context.Context, delivery amqp.Delivery) {
	eventType := extractHeader(delivery.Headers, "ce-type")

	ctx, span := tracer.Start(ctx, "audit.process_event",
		trace.WithSpanKind(trace.SpanKindConsumer),
		trace.WithAttributes(
			semconv.MessagingSystemRabbitmq,
			attribute.String("messaging.operation.type", "process"),
			attribute.String("messaging.cloudevents.type", eventType),
		),
	)
	defer span.End()

	if err := c.processEvent(ctx, delivery.Body, eventType); err != nil {
		slog.Error("failed to process event", "error", err, "event_type", eventType)
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to process event")
		delivery.Nack(false, false)
		return
	}

	delivery.Ack(false)
}

func (c *Consumer) processEvent(ctx context.Context, body []byte, eventType string) error {
	var envelope cloudevents.CloudEvent[json.RawMessage]
	if err := json.Unmarshal(body, &envelope); err != nil {
		return fmt.Errorf("failed to unmarshal cloud event: %w", err)
	}

	actorID := extractActorID(envelope.Data)
	targetID := extractTargetID(envelope.Data)

	log := &store.AuditLog{
		EventType: envelope.Type,
		EventID:   envelope.ID,
		Source:    envelope.Source,
		ActorID:   actorID,
		TargetID:  targetID,
		Metadata:  envelope.Data,
		CreatedAt: envelope.Time,
	}

	if err := c.store.InsertAuditLog(ctx, log); err != nil {
		return fmt.Errorf("failed to insert audit log: %w", err)
	}

	slog.Debug("audit log created", "event_type", envelope.Type, "event_id", envelope.ID)
	return nil
}

func extractHeader(headers amqp.Table, key string) string {
	if v, ok := headers[key]; ok {
		if s, ok := v.(string); ok {
			return s
		}
	}
	return ""
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
