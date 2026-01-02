package config

import (
	"fmt"
	"os"
)

type Config struct {
	HTTPAddress          string
	DatabaseURL          string
	RabbitMQURL          string
	RabbitMQManagementURL string
	OTLPEndpoint         string
}

func Load() (*Config, error) {
	cfg := &Config{
		HTTPAddress:          getEnv("HTTP_ADDRESS", ":5005"),
		DatabaseURL:          os.Getenv("DATABASE_URL"),
		RabbitMQURL:          getEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672"),
		RabbitMQManagementURL: getEnv("RABBITMQ_MANAGEMENT_URL", "http://guest:guest@localhost:15672"),
		OTLPEndpoint:         getEnv("OTEL_EXPORTER_OTLP_ENDPOINT", "localhost:4318"),
	}

	if cfg.DatabaseURL == "" {
		return nil, fmt.Errorf("DATABASE_URL is required")
	}

	return cfg, nil
}

func getEnv(key, defaultVal string) string {
	if val := os.Getenv(key); val != "" {
		return val
	}
	return defaultVal
}
