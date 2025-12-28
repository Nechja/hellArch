package events

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"time"

	"github.com/google/uuid"
	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/auth"
	amqp "github.com/rabbitmq/amqp091-go"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"go.opentelemetry.io/otel/trace"
)

const (
	ExchangeName = "hellarch.events"
	ExchangeType = "topic"
)

// EventType constants for routing
const (
	EventTypeUserCreated     = "hellarch.auth.user.created.v1"
	EventTypeUserUpdated     = "hellarch.auth.user.updated.v1"
	EventTypeUserDeleted     = "hellarch.auth.user.deleted.v1"
	EventTypeSessionCreated  = "hellarch.auth.session.created.v1"
	EventTypeSessionEnded    = "hellarch.auth.session.ended.v1"
	EventTypeAccountLinked   = "hellarch.auth.account.linked.v1"
	EventTypeAccountUnlinked = "hellarch.auth.account.unlinked.v1"
)

var tracer = otel.Tracer("hellarch/auth/events")

type Publisher struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	source  string
}

// amqpCarrier implements propagation.TextMapCarrier for AMQP headers
type amqpCarrier amqp.Table

func (c amqpCarrier) Get(key string) string {
	if v, ok := c[key]; ok {
		if s, ok := v.(string); ok {
			return s
		}
	}
	return ""
}

func (c amqpCarrier) Set(key, value string) {
	c[key] = value
}

func (c amqpCarrier) Keys() []string {
	keys := make([]string, 0, len(c))
	for k := range c {
		keys = append(keys, k)
	}
	return keys
}

func NewPublisher(ctx context.Context, amqpURL, source string) (*Publisher, error) {
	conn, err := amqp.Dial(amqpURL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		_ = conn.Close()
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	if err := ch.ExchangeDeclare(
		ExchangeName,
		ExchangeType,
		true,  // durable
		false, // auto-deleted
		false, // internal
		false, // no-wait
		nil,   // arguments
	); err != nil {
		_ = ch.Close()
		_ = conn.Close()
		return nil, fmt.Errorf("failed to declare exchange: %w", err)
	}

	slog.Info("connected to RabbitMQ", "exchange", ExchangeName)

	return &Publisher{
		conn:    conn,
		channel: ch,
		source:  source,
	}, nil
}

func (p *Publisher) Close() error {
	if err := p.channel.Close(); err != nil {
		return err
	}
	return p.conn.Close()
}

func (p *Publisher) Publish(ctx context.Context, eventType string, data any, traceParent string) error {
	ctx, span := tracer.Start(ctx, "rabbitmq.publish",
		trace.WithSpanKind(trace.SpanKindProducer),
		trace.WithAttributes(
			semconv.MessagingSystemRabbitmq,
			semconv.MessagingDestinationName(ExchangeName),
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

	if traceParent != "" {
		event.Extensions = map[string]string{
			"traceparent": traceParent,
		}
	}

	body, err := json.Marshal(event)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, "failed to marshal event")
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	// Use event type as routing key (dots work well with topic exchanges)
	routingKey := eventType

	headers := amqp.Table{
		"ce-specversion": event.SpecVersion,
		"ce-id":          event.ID,
		"ce-source":      event.Source,
		"ce-type":        event.Type,
	}

	otel.GetTextMapPropagator().Inject(ctx, amqpCarrier(headers))

	if err := p.channel.PublishWithContext(
		ctx,
		ExchangeName,
		routingKey,
		false, // mandatory
		false, // immediate
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

type UserCreated = auth.UserCreated
type SessionCreated = auth.SessionCreated
type AccountLinked = auth.AccountLinked

func (p *Publisher) PublishUserCreated(ctx context.Context, data UserCreated) error {
	return p.Publish(ctx, EventTypeUserCreated, data, "")
}

func (p *Publisher) PublishSessionCreated(ctx context.Context, data SessionCreated) error {
	return p.Publish(ctx, EventTypeSessionCreated, data, "")
}

func (p *Publisher) PublishAccountLinked(ctx context.Context, data AccountLinked) error {
	return p.Publish(ctx, EventTypeAccountLinked, data, "")
}
