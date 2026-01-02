package rabbitmq

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"time"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/infra"
	amqp "github.com/rabbitmq/amqp091-go"
)

const (
	ExchangeName = "hellarch.events"
	QueueName    = "infra-worker"

	EventTypeServiceUnhealthy = "hellarch.infra.service.unhealthy.v1"
	EventTypeServiceRecovered = "hellarch.infra.service.recovered.v1"
)

type EventHandler interface {
	HandleServiceUnhealthy(ctx context.Context, event infra.ServiceUnhealthy) error
	HandleServiceRecovered(ctx context.Context, event infra.ServiceRecovered) error
}

type Consumer struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	handler EventHandler
	logger  *slog.Logger
}

func NewConsumer(url string, handler EventHandler) (*Consumer, error) {
	logger := slog.Default().With("component", "rabbitmq")

	var conn *amqp.Connection
	var err error

	// Retry connection with backoff
	for i := 0; i < 10; i++ {
		conn, err = amqp.Dial(url)
		if err == nil {
			break
		}
		logger.Warn("failed to connect to rabbitmq, retrying...", "attempt", i+1, "error", err)
		time.Sleep(time.Duration(i+1) * time.Second)
	}
	if err != nil {
		return nil, fmt.Errorf("failed to connect to rabbitmq after retries: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	// Declare exchange
	err = ch.ExchangeDeclare(
		ExchangeName,
		"topic",
		true,  // durable
		false, // auto-deleted
		false, // internal
		false, // no-wait
		nil,
	)
	if err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to declare exchange: %w", err)
	}

	// Declare queue
	q, err := ch.QueueDeclare(
		QueueName,
		true,  // durable
		false, // delete when unused
		false, // exclusive
		false, // no-wait
		nil,
	)
	if err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to declare queue: %w", err)
	}

	// Bind to infra events
	err = ch.QueueBind(q.Name, "hellarch.infra.#", ExchangeName, false, nil)
	if err != nil {
		ch.Close()
		conn.Close()
		return nil, fmt.Errorf("failed to bind queue: %w", err)
	}

	logger.Info("connected to rabbitmq", "queue", QueueName)

	return &Consumer{
		conn:    conn,
		channel: ch,
		handler: handler,
		logger:  logger,
	}, nil
}

func (c *Consumer) Start(ctx context.Context) error {
	msgs, err := c.channel.Consume(
		QueueName,
		"",    // consumer tag
		false, // auto-ack
		false, // exclusive
		false, // no-local
		false, // no-wait
		nil,
	)
	if err != nil {
		return fmt.Errorf("failed to start consuming: %w", err)
	}

	c.logger.Info("started consuming messages")

	for {
		select {
		case <-ctx.Done():
			c.logger.Info("context cancelled, stopping consumer")
			return nil
		case msg, ok := <-msgs:
			if !ok {
				return fmt.Errorf("channel closed")
			}
			c.handleMessage(ctx, msg)
		}
	}
}

func (c *Consumer) handleMessage(ctx context.Context, msg amqp.Delivery) {
	c.logger.Debug("received message", "routing_key", msg.RoutingKey)

	var envelope struct {
		Type string `json:"type"`
	}
	if err := json.Unmarshal(msg.Body, &envelope); err != nil {
		c.logger.Error("failed to parse event envelope", "error", err)
		msg.Nack(false, false)
		return
	}

	var err error
	switch envelope.Type {
	case EventTypeServiceUnhealthy:
		err = c.handleServiceUnhealthy(ctx, msg.Body)
	case EventTypeServiceRecovered:
		err = c.handleServiceRecovered(ctx, msg.Body)
	default:
		c.logger.Debug("unknown event type, ignoring", "type", envelope.Type)
		msg.Ack(false)
		return
	}

	if err != nil {
		c.logger.Error("failed to handle event", "type", envelope.Type, "error", err)
		msg.Nack(false, false)
		return
	}

	msg.Ack(false)
}

func (c *Consumer) handleServiceUnhealthy(ctx context.Context, body []byte) error {
	var event cloudevents.CloudEvent[infra.ServiceUnhealthy]
	if err := json.Unmarshal(body, &event); err != nil {
		return fmt.Errorf("failed to unmarshal ServiceUnhealthy: %w", err)
	}
	return c.handler.HandleServiceUnhealthy(ctx, event.Data)
}

func (c *Consumer) handleServiceRecovered(ctx context.Context, body []byte) error {
	var event cloudevents.CloudEvent[infra.ServiceRecovered]
	if err := json.Unmarshal(body, &event); err != nil {
		return fmt.Errorf("failed to unmarshal ServiceRecovered: %w", err)
	}
	return c.handler.HandleServiceRecovered(ctx, event.Data)
}

func (c *Consumer) Close() error {
	if c.channel != nil {
		c.channel.Close()
	}
	if c.conn != nil {
		c.conn.Close()
	}
	return nil
}
