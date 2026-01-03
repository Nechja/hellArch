package config

import (
	"fmt"
	"strconv"

	sharedconfig "github.com/nechja/hellarch/golibs/config"
)

type Config struct {
	HTTPAddress    string
	DatabaseURL    string
	RabbitMQURL    string
	OTLPEndpoint   string
	MinioEndpoint  string
	MinioAccessKey string
	MinioSecretKey string
	MinioBucket    string
	MinioUseSSL    bool
	MaxUploadSize  int64
}

func Load() (*Config, error) {
	maxUploadStr := sharedconfig.GetEnv("MAX_UPLOAD_SIZE", "1073741824") // 1GB default
	maxUpload, err := strconv.ParseInt(maxUploadStr, 10, 64)
	if err != nil {
		maxUpload = 1073741824
	}

	useSSL := sharedconfig.GetEnv("MINIO_USE_SSL", "false") == "true"

	cfg := &Config{
		HTTPAddress:    sharedconfig.GetEnv("HTTP_ADDRESS", ":5006"),
		DatabaseURL:    sharedconfig.GetEnv("DATABASE_URL", ""),
		RabbitMQURL:    sharedconfig.GetEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672"),
		OTLPEndpoint:   sharedconfig.GetEnv("OTEL_EXPORTER_OTLP_ENDPOINT", "localhost:4318"),
		MinioEndpoint:  sharedconfig.GetEnv("MINIO_ENDPOINT", "localhost:9000"),
		MinioAccessKey: sharedconfig.GetEnv("MINIO_ACCESS_KEY", "hellarch"),
		MinioSecretKey: sharedconfig.GetEnv("MINIO_SECRET_KEY", "hellarch"),
		MinioBucket:    sharedconfig.GetEnv("MINIO_BUCKET", "hellarch-files"),
		MinioUseSSL:    useSSL,
		MaxUploadSize:  maxUpload,
	}

	if cfg.DatabaseURL == "" {
		return nil, fmt.Errorf("DATABASE_URL is required")
	}

	if cfg.MinioSecretKey == "" {
		return nil, fmt.Errorf("MINIO_SECRET_KEY is required")
	}

	return cfg, nil
}
