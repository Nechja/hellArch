package config

import (
	"errors"
	"os"
	"time"
)

type Config struct {
	EventsAPIURL   string
	RabbitMQURL    string
	OTLPEndpoint   string
	RequestTimeout time.Duration
	MaxRetries     int
}

func Load() (*Config, error) {
	cfg := &Config{
		EventsAPIURL:   os.Getenv("EVENTS_API_URL"),
		RabbitMQURL:    os.Getenv("RABBITMQ_URL"),
		OTLPEndpoint:   os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT"),
		RequestTimeout: 30 * time.Second,
		MaxRetries:     3,
	}

	if cfg.EventsAPIURL == "" {
		return nil, errors.New("EVENTS_API_URL is required")
	}

	if cfg.RabbitMQURL == "" {
		cfg.RabbitMQURL = "amqp://guest:guest@localhost:5672"
	}

	if cfg.OTLPEndpoint == "" {
		cfg.OTLPEndpoint = "localhost:4318"
	}

	return cfg, nil
}
