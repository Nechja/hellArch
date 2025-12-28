package events

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"time"

	"github.com/google/uuid"
	amqp "github.com/rabbitmq/amqp091-go"
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

// Publisher handles publishing CloudEvents to RabbitMQ
type Publisher struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	source  string
}

// NewPublisher creates a new RabbitMQ event publisher
func NewPublisher(ctx context.Context, amqpURL, source string) (*Publisher, error) {
	conn, err := amqp.Dial(amqpURL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	// Declare the topic exchange
	if err := ch.ExchangeDeclare(
		ExchangeName,
		ExchangeType,
		true,  // durable
		false, // auto-deleted
		false, // internal
		false, // no-wait
		nil,   // arguments
	); err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to declare exchange: %w", err)
	}

	slog.Info("connected to RabbitMQ", "exchange", ExchangeName)

	return &Publisher{
		conn:    conn,
		channel: ch,
		source:  source,
	}, nil
}

// Close closes the RabbitMQ connection
func (p *Publisher) Close() error {
	if err := p.channel.Close(); err != nil {
		return err
	}
	return p.conn.Close()
}

// CloudEvent represents a CloudEvents 1.0 compliant event
type CloudEvent struct {
	SpecVersion     string            `json:"specversion"`
	ID              string            `json:"id"`
	Source          string            `json:"source"`
	Type            string            `json:"type"`
	DataContentType string            `json:"datacontenttype"`
	Time            string            `json:"time"`
	Data            any               `json:"data"`
	Extensions      map[string]string `json:"extensions,omitempty"`
}

// Publish publishes a CloudEvent to RabbitMQ
func (p *Publisher) Publish(ctx context.Context, eventType string, data any, traceParent string) error {
	event := CloudEvent{
		SpecVersion:     "1.0",
		ID:              uuid.New().String(),
		Source:          p.source,
		Type:            eventType,
		DataContentType: "application/json",
		Time:            time.Now().UTC().Format(time.RFC3339),
		Data:            data,
	}

	if traceParent != "" {
		event.Extensions = map[string]string{
			"traceparent": traceParent,
		}
	}

	body, err := json.Marshal(event)
	if err != nil {
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	// Use event type as routing key (dots work well with topic exchanges)
	routingKey := eventType

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
			Headers: amqp.Table{
				"ce-specversion": event.SpecVersion,
				"ce-id":          event.ID,
				"ce-source":      event.Source,
				"ce-type":        event.Type,
			},
		},
	); err != nil {
		return fmt.Errorf("failed to publish event: %w", err)
	}

	slog.Debug("published event", "type", eventType, "id", event.ID)
	return nil
}

// UserCreatedData represents the data payload for user.created events
type UserCreatedData struct {
	UserID         string `json:"user_id"`
	Email          string `json:"email"`
	Username       string `json:"username"`
	Provider       string `json:"provider"`
	ProviderUserID string `json:"provider_user_id"`
	AvatarURL      string `json:"avatar_url"`
	CreatedAt      string `json:"created_at"`
}

// PublishUserCreated publishes a user.created event
func (p *Publisher) PublishUserCreated(ctx context.Context, data UserCreatedData) error {
	return p.Publish(ctx, EventTypeUserCreated, data, "")
}

// SessionCreatedData represents the data payload for session.created events
type SessionCreatedData struct {
	SessionID string `json:"session_id"`
	UserID    string `json:"user_id"`
	IPAddress string `json:"ip_address"`
	UserAgent string `json:"user_agent"`
	CreatedAt string `json:"created_at"`
	ExpiresAt string `json:"expires_at"`
}

// PublishSessionCreated publishes a session.created event
func (p *Publisher) PublishSessionCreated(ctx context.Context, data SessionCreatedData) error {
	return p.Publish(ctx, EventTypeSessionCreated, data, "")
}

// AccountLinkedData represents the data payload for account.linked events
type AccountLinkedData struct {
	UserID         string `json:"user_id"`
	Provider       string `json:"provider"`
	ProviderUserID string `json:"provider_user_id"`
	Email          string `json:"email"`
	LinkedAt       string `json:"linked_at"`
}

// PublishAccountLinked publishes an account.linked event
func (p *Publisher) PublishAccountLinked(ctx context.Context, data AccountLinkedData) error {
	return p.Publish(ctx, EventTypeAccountLinked, data, "")
}
