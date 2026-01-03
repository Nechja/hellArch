package config

import (
	"fmt"

	sharedconfig "github.com/nechja/hellarch/golibs/config"
)

type Config struct {
	HTTPAddress           string
	DatabaseURL           string
	RabbitMQURL           string
	RabbitMQManagementURL string
	OTLPEndpoint          string
}

func Load() (*Config, error) {
	cfg := &Config{
		HTTPAddress:           sharedconfig.GetEnv("HTTP_ADDRESS", ":5005"),
		DatabaseURL:           sharedconfig.GetEnv("DATABASE_URL", ""),
		RabbitMQURL:           sharedconfig.GetEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672"),
		RabbitMQManagementURL: sharedconfig.GetEnv("RABBITMQ_MANAGEMENT_URL", "http://guest:guest@localhost:15672"),
		OTLPEndpoint:          sharedconfig.GetEnv("OTEL_EXPORTER_OTLP_ENDPOINT", "localhost:4318"),
	}

	if cfg.DatabaseURL == "" {
		return nil, fmt.Errorf("DATABASE_URL is required")
	}

	return cfg, nil
}
