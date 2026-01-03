package rabbitmq

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"time"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	amqp "github.com/rabbitmq/amqp091-go"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"go.opentelemetry.io/otel/trace"
)

// Consumer consumes CloudEvents from RabbitMQ with automatic reconnection.
type Consumer struct {
	cfg    ConsumerConfig
	conn   *Connection
	tracer trace.Tracer
}

// ConsumerConfig configures a Consumer.
type ConsumerConfig struct {
	URL          string
	QueueName    string
	RoutingKey   string
	ExchangeName string
	ConsumerTag  string
	TracerName   string

	// Reconnection settings
	MaxReconnectAttempts int           // 0 = unlimited
	InitialBackoff       time.Duration // default 1s
	MaxBackoff           time.Duration // default 30s
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
	if cfg.InitialBackoff == 0 {
		cfg.InitialBackoff = time.Second
	}
	if cfg.MaxBackoff == 0 {
		cfg.MaxBackoff = 30 * time.Second
	}

	consumer := &Consumer{
		cfg:    cfg,
		tracer: otel.Tracer(cfg.TracerName),
	}

	if err := consumer.connect(ctx); err != nil {
		return nil, err
	}

	return consumer, nil
}

func (c *Consumer) connect(ctx context.Context) error {
	conn, err := NewConnection(ConnectionConfig{
		URL:          c.cfg.URL,
		ExchangeName: c.cfg.ExchangeName,
	})
	if err != nil {
		return err
	}

	ch := conn.Channel()

	// Declare DLQ exchange
	if err := ch.ExchangeDeclare(
		"hellarch.events.dlq",
		"fanout",
		true,  // durable
		false, // auto-deleted
		false, // internal
		false, // no-wait
		nil,
	); err != nil {
		conn.Close()
		return fmt.Errorf("failed to declare DLQ exchange: %w", err)
	}

	// Declare queue with DLQ routing
	args := amqp.Table{
		"x-dead-letter-exchange": "hellarch.events.dlq",
	}

	if _, err := ch.QueueDeclare(
		c.cfg.QueueName,
		true,  // durable
		false, // auto-delete
		false, // exclusive
		false, // no-wait
		args,
	); err != nil {
		conn.Close()
		return fmt.Errorf("failed to declare queue: %w", err)
	}

	if err := ch.QueueBind(
		c.cfg.QueueName,
		c.cfg.RoutingKey,
		c.cfg.ExchangeName,
		false,
		nil,
	); err != nil {
		conn.Close()
		return fmt.Errorf("failed to bind queue: %w", err)
	}

	c.conn = conn
	slog.Info("consumer connected to RabbitMQ",
		"queue", c.cfg.QueueName,
		"routing_key", c.cfg.RoutingKey,
		"dlq_exchange", "hellarch.events.dlq")

	return nil
}

// Close closes the underlying connection.
func (c *Consumer) Close() error {
	if c.conn != nil {
		return c.conn.Close()
	}
	return nil
}

// IsHealthy returns true if the consumer has an active connection.
func (c *Consumer) IsHealthy() bool {
	if c.conn == nil {
		return false
	}
	return !c.conn.IsClosed()
}

// Run starts consuming messages with automatic reconnection.
// Blocks until the context is cancelled.
func (c *Consumer) Run(ctx context.Context, handler MessageHandler) error {
	backoff := c.cfg.InitialBackoff
	attempts := 0

	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}

		err := c.consume(ctx, handler)
		if err == nil || ctx.Err() != nil {
			return err
		}

		attempts++
		if c.cfg.MaxReconnectAttempts > 0 && attempts >= c.cfg.MaxReconnectAttempts {
			return fmt.Errorf("max reconnection attempts (%d) reached: %w", c.cfg.MaxReconnectAttempts, err)
		}

		slog.Warn("consumer disconnected, attempting reconnect",
			"error", err,
			"attempt", attempts,
			"backoff", backoff)

		select {
		case <-ctx.Done():
			return ctx.Err()
		case <-time.After(backoff):
		}

		// Exponential backoff with cap
		backoff = time.Duration(float64(backoff) * 1.5)
		if backoff > c.cfg.MaxBackoff {
			backoff = c.cfg.MaxBackoff
		}

		// Attempt reconnection
		if err := c.reconnect(ctx); err != nil {
			slog.Error("failed to reconnect", "error", err)
			continue
		}

		// Reset backoff on successful reconnect
		backoff = c.cfg.InitialBackoff
		attempts = 0
		slog.Info("consumer reconnected successfully", "queue", c.cfg.QueueName)
	}
}

func (c *Consumer) reconnect(ctx context.Context) error {
	if c.conn != nil {
		c.conn.Close()
		c.conn = nil
	}
	return c.connect(ctx)
}

func (c *Consumer) consume(ctx context.Context, handler MessageHandler) error {
	if c.conn == nil {
		return fmt.Errorf("not connected")
	}

	deliveries, err := c.conn.Channel().Consume(
		c.cfg.QueueName,
		c.cfg.ConsumerTag,
		false, // auto-ack
		false, // exclusive
		false, // no-local
		false, // no-wait
		nil,   // arguments
	)
	if err != nil {
		return fmt.Errorf("failed to start consuming: %w", err)
	}

	slog.Info("starting to consume events", "queue", c.cfg.QueueName)

	for {
		select {
		case <-ctx.Done():
			return nil
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

	// Extract trace context from headers
	propagator := otel.GetTextMapPropagator()
	ctx = propagator.Extract(ctx, AMQPCarrier(delivery.Headers))

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
