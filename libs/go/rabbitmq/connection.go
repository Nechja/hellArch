package rabbitmq

import (
	"fmt"
	"log/slog"

	amqp "github.com/rabbitmq/amqp091-go"
)

const (
	DefaultExchangeName = "hellarch.events"
	DefaultExchangeType = "topic"
)

// Connection wraps an AMQP connection and channel.
type Connection struct {
	conn    *amqp.Connection
	channel *amqp.Channel
}

// ConnectionConfig configures a RabbitMQ connection.
type ConnectionConfig struct {
	URL          string
	ExchangeName string
}

// NewConnection creates a new RabbitMQ connection with the exchange declared.
func NewConnection(cfg ConnectionConfig) (*Connection, error) {
	if cfg.ExchangeName == "" {
		cfg.ExchangeName = DefaultExchangeName
	}

	conn, err := amqp.Dial(cfg.URL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, fmt.Errorf("failed to open channel: %w", err)
	}

	if err := ch.ExchangeDeclare(
		cfg.ExchangeName,
		DefaultExchangeType,
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

	slog.Info("connected to RabbitMQ", "exchange", cfg.ExchangeName)

	return &Connection{
		conn:    conn,
		channel: ch,
	}, nil
}

// Channel returns the underlying AMQP channel.
func (c *Connection) Channel() *amqp.Channel {
	return c.channel
}

// Close closes the channel and connection.
func (c *Connection) Close() error {
	if err := c.channel.Close(); err != nil {
		return err
	}
	return c.conn.Close()
}
