package rabbitmq

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	amqp "github.com/rabbitmq/amqp091-go"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"go.opentelemetry.io/otel/trace"
)

// Consumer consumes CloudEvents from RabbitMQ.
type Consumer struct {
	conn         *Connection
	queueName    string
	routingKey   string
	exchangeName string
	consumerTag  string
	tracer       trace.Tracer
}

// ConsumerConfig configures a Consumer.
type ConsumerConfig struct {
	URL          string
	QueueName    string
	RoutingKey   string
	ExchangeName string
	ConsumerTag  string
	TracerName   string
}

// MessageHandler processes a CloudEvent message.
// Return nil to ack the message, or an error to nack it.
type MessageHandler func(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error

// NewConsumer creates a new Consumer.
func NewConsumer(ctx context.Context, cfg ConsumerConfig) (*Consumer, error) {
	if cfg.ExchangeName == "" {
		cfg.ExchangeName = DefaultExchangeName
	}
	if cfg.RoutingKey == "" {
		cfg.RoutingKey = "hellarch.#"
	}
	if cfg.ConsumerTag == "" {
		cfg.ConsumerTag = cfg.QueueName + "-consumer"
	}
	if cfg.TracerName == "" {
		cfg.TracerName = cfg.QueueName
	}

	conn, err := NewConnection(ConnectionConfig{
		URL:          cfg.URL,
		ExchangeName: cfg.ExchangeName,
	})
	if err != nil {
		return nil, err
	}

	ch := conn.Channel()

	if _, err := ch.QueueDeclare(
		cfg.QueueName,
		true,  // durable
		false, // auto-delete
		false, // exclusive
		false, // no-wait
		nil,   // arguments
	); err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to declare queue: %w", err)
	}

	if err := ch.QueueBind(
		cfg.QueueName,
		cfg.RoutingKey,
		cfg.ExchangeName,
		false,
		nil,
	); err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to bind queue: %w", err)
	}

	slog.Info("consumer connected to RabbitMQ", "queue", cfg.QueueName, "routing_key", cfg.RoutingKey)

	return &Consumer{
		conn:         conn,
		queueName:    cfg.QueueName,
		routingKey:   cfg.RoutingKey,
		exchangeName: cfg.ExchangeName,
		consumerTag:  cfg.ConsumerTag,
		tracer:       otel.Tracer(cfg.TracerName),
	}, nil
}

// Close closes the underlying connection.
func (c *Consumer) Close() error {
	return c.conn.Close()
}

// Run starts consuming messages and calls the handler for each.
// Blocks until the context is cancelled.
func (c *Consumer) Run(ctx context.Context, handler MessageHandler) error {
	deliveries, err := c.conn.Channel().Consume(
		c.queueName,
		c.consumerTag,
		false, // auto-ack
		false, // exclusive
		false, // no-local
		false, // no-wait
		nil,   // arguments
	)
	if err != nil {
		return fmt.Errorf("failed to start consuming: %w", err)
	}

	slog.Info("starting to consume events", "queue", c.queueName)

	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		case delivery, ok := <-deliveries:
			if !ok {
				return fmt.Errorf("delivery channel closed")
			}
			c.handleDelivery(ctx, delivery, handler)
		}
	}
}

func (c *Consumer) handleDelivery(ctx context.Context, delivery amqp.Delivery, handler MessageHandler) {
	eventType := ExtractHeader(delivery.Headers, "ce-type")

	ctx, span := c.tracer.Start(ctx, "process_event",
		trace.WithSpanKind(trace.SpanKindConsumer),
		trace.WithAttributes(
			semconv.MessagingSystemRabbitmq,
			attribute.String("messaging.operation.type", "process"),
			attribute.String("messaging.cloudevents.type", eventType),
		),
	)
	defer span.End()

	var event cloudevents.CloudEvent[json.RawMessage]
	if err := json.Unmarshal(delivery.Body, &event); err != nil {
		slog.Error("failed to unmarshal cloud event", "error", err)
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to unmarshal event")
		delivery.Nack(false, false)
		return
	}

	if err := handler(ctx, event); err != nil {
		slog.Error("failed to process event", "error", err, "event_type", eventType)
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to process event")
		delivery.Nack(false, false)
		return
	}

	delivery.Ack(false)
}
