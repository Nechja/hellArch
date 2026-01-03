package config

import (
	"errors"

	sharedconfig "github.com/nechja/hellarch/golibs/config"
)

type Config struct {
	EventsAPIURL string
	RabbitMQURL  string
	OTLPEndpoint string
}

func Load() (*Config, error) {
	cfg := &Config{
		EventsAPIURL: sharedconfig.GetEnv("EVENTS_API_URL", ""),
		RabbitMQURL:  sharedconfig.GetEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672"),
		OTLPEndpoint: sharedconfig.GetEnv("OTEL_EXPORTER_OTLP_ENDPOINT", "localhost:4318"),
	}

	if cfg.EventsAPIURL == "" {
		return nil, errors.New("EVENTS_API_URL is required")
	}

	return cfg, nil
}
