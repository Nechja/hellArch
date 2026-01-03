package rabbitmq

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"time"

	"github.com/google/uuid"
	cloudevents "github.com/nechja/hellarch/cloudevents"
	amqp "github.com/rabbitmq/amqp091-go"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"go.opentelemetry.io/otel/trace"
)

// Publisher publishes CloudEvents to RabbitMQ.
type Publisher struct {
	conn         *Connection
	source       string
	exchangeName string
	tracer       trace.Tracer
}

// PublisherConfig configures a Publisher.
type PublisherConfig struct {
	URL          string
	Source       string
	ExchangeName string
	TracerName   string
}

// NewPublisher creates a new Publisher.
func NewPublisher(ctx context.Context, cfg PublisherConfig) (*Publisher, error) {
	if cfg.ExchangeName == "" {
		cfg.ExchangeName = DefaultExchangeName
	}
	if cfg.TracerName == "" {
		cfg.TracerName = "rabbitmq/publisher"
	}

	conn, err := NewConnection(ConnectionConfig{
		URL:          cfg.URL,
		ExchangeName: cfg.ExchangeName,
	})
	if err != nil {
		return nil, err
	}

	return &Publisher{
		conn:         conn,
		source:       cfg.Source,
		exchangeName: cfg.ExchangeName,
		tracer:       otel.Tracer(cfg.TracerName),
	}, nil
}

// Close closes the underlying connection.
func (p *Publisher) Close() error {
	return p.conn.Close()
}

// Publish publishes a CloudEvent with the given event type and data.
func (p *Publisher) Publish(ctx context.Context, eventType string, data any) error {
	ctx, span := p.tracer.Start(ctx, "rabbitmq.publish",
		trace.WithSpanKind(trace.SpanKindProducer),
		trace.WithAttributes(
			semconv.MessagingSystemRabbitmq,
			semconv.MessagingDestinationName(p.exchangeName),
			semconv.MessagingRabbitmqDestinationRoutingKey(eventType),
			attribute.String("messaging.cloudevents.type", eventType),
		),
	)
	defer span.End()

	event := cloudevents.CloudEvent[any]{
		SpecVersion:     "1.0",
		ID:              uuid.New().String(),
		Source:          p.source,
		Type:            eventType,
		DataContentType: "application/json",
		Time:            time.Now().UTC(),
		Data:            data,
	}

	body, err := json.Marshal(event)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to marshal event")
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	headers := amqp.Table{
		"ce-specversion": event.SpecVersion,
		"ce-id":          event.ID,
		"ce-source":      event.Source,
		"ce-type":        event.Type,
	}

	otel.GetTextMapPropagator().Inject(ctx, AMQPCarrier(headers))

	if err := p.conn.Channel().PublishWithContext(
		ctx,
		p.exchangeName,
		eventType, // routing key
		false,     // mandatory
		false,     // immediate
		amqp.Publishing{
			ContentType:  "application/cloudevents+json",
			DeliveryMode: amqp.Persistent,
			MessageId:    event.ID,
			Timestamp:    time.Now(),
			Body:         body,
			Headers:      headers,
		},
	); err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to publish event")
		return fmt.Errorf("failed to publish event: %w", err)
	}

	span.SetAttributes(attribute.String("messaging.message.id", event.ID))
	slog.Debug("published event", "type", eventType, "id", event.ID)
	return nil
}

// PublishRaw publishes a pre-built CloudEvent.
func (p *Publisher) PublishRaw(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error {
	ctx, span := p.tracer.Start(ctx, "rabbitmq.publish",
		trace.WithSpanKind(trace.SpanKindProducer),
		trace.WithAttributes(
			semconv.MessagingSystemRabbitmq,
			semconv.MessagingDestinationName(p.exchangeName),
			semconv.MessagingRabbitmqDestinationRoutingKey(event.Type),
			attribute.String("messaging.cloudevents.type", event.Type),
		),
	)
	defer span.End()

	body, err := json.Marshal(event)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to marshal event")
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	headers := amqp.Table{
		"ce-specversion": event.SpecVersion,
		"ce-id":          event.ID,
		"ce-source":      event.Source,
		"ce-type":        event.Type,
	}

	otel.GetTextMapPropagator().Inject(ctx, AMQPCarrier(headers))

	if err := p.conn.Channel().PublishWithContext(
		ctx,
		p.exchangeName,
		event.Type, // routing key
		false,      // mandatory
		false,      // immediate
		amqp.Publishing{
			ContentType:  "application/cloudevents+json",
			DeliveryMode: amqp.Persistent,
			MessageId:    event.ID,
			Timestamp:    time.Now(),
			Body:         body,
			Headers:      headers,
		},
	); err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to publish event")
		return fmt.Errorf("failed to publish event: %w", err)
	}

	span.SetAttributes(attribute.String("messaging.message.id", event.ID))
	slog.Debug("published event", "type", event.Type, "id", event.ID)
	return nil
}
