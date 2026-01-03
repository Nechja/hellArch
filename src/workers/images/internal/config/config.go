package config

import (
	"os"
	"strconv"
)

type Config struct {
	RabbitMQURL    string
	OTLPEndpoint   string
	MinioEndpoint  string
	MinioAccessKey string
	MinioSecretKey string
	MinioBucket    string
	MinioUseSSL    bool
	FilesAPIURL    string
	ThumbnailSize  int
	HealthAddress  string
}

func Load() *Config {
	thumbnailSize := 256
	if s := os.Getenv("IMAGES_THUMBNAIL_SIZE"); s != "" {
		if v, err := strconv.Atoi(s); err == nil && v > 0 {
			thumbnailSize = v
		}
	}

	useSSL := false
	if os.Getenv("IMAGES_MINIO_USE_SSL") == "true" {
		useSSL = true
	}

	healthAddress := os.Getenv("IMAGES_HEALTH_ADDRESS")
	if healthAddress == "" {
		healthAddress = ":8081"
	}

	return &Config{
		RabbitMQURL:    os.Getenv("IMAGES_RABBITMQ_URL"),
		OTLPEndpoint:   os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT"),
		MinioEndpoint:  os.Getenv("IMAGES_MINIO_ENDPOINT"),
		MinioAccessKey: os.Getenv("IMAGES_MINIO_ACCESS_KEY"),
		MinioSecretKey: os.Getenv("IMAGES_MINIO_SECRET_KEY"),
		MinioBucket:    os.Getenv("IMAGES_MINIO_BUCKET"),
		MinioUseSSL:    useSSL,
		FilesAPIURL:    os.Getenv("IMAGES_FILES_API_URL"),
		ThumbnailSize:  thumbnailSize,
		HealthAddress:  healthAddress,
	}
}
