package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/nechja/hellarch/golibs/health"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/workers/images/internal/config"
	"github.com/nechja/hellarch/workers/images/internal/consumer"
	"github.com/nechja/hellarch/workers/images/internal/processor"
	"github.com/nechja/hellarch/workers/images/internal/storage"
)

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Setup logging
	logLevel := os.Getenv("LOG_LEVEL")
	var level slog.Level
	switch logLevel {
	case "debug":
		level = slog.LevelDebug
	case "warn", "warning":
		level = slog.LevelWarn
	case "error":
		level = slog.LevelError
	default:
		level = slog.LevelInfo
	}

	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{Level: level}))
	slog.SetDefault(logger)

	// Load config
	cfg := config.Load()

	// Setup telemetry
	shutdown, err := telemetry.Setup(ctx, "images-worker")
	if err != nil {
		slog.Warn("failed to setup telemetry", "error", err)
	} else {
		defer shutdown(ctx)
	}

	// Connect to MinIO
	minioStorage, err := storage.NewMinioStorage(ctx, storage.MinioConfig{
		Endpoint:  cfg.MinioEndpoint,
		AccessKey: cfg.MinioAccessKey,
		SecretKey: cfg.MinioSecretKey,
		Bucket:    cfg.MinioBucket,
		UseSSL:    cfg.MinioUseSSL,
	})
	if err != nil {
		slog.Error("failed to connect to MinIO", "error", err)
		os.Exit(1)
	}
	slog.Info("connected to MinIO", "bucket", cfg.MinioBucket)

	// Connect to RabbitMQ for publishing
	var publisher *rabbitmq.Publisher
	publisher, err = rabbitmq.NewPublisher(ctx, rabbitmq.PublisherConfig{
		URL:        cfg.RabbitMQURL,
		Source:     "hellarch/images-worker",
		TracerName: "images/publisher",
	})
	if err != nil {
		slog.Error("failed to connect to RabbitMQ publisher", "error", err)
		os.Exit(1)
	}
	defer publisher.Close()
	slog.Info("connected to RabbitMQ publisher")

	// Create processor
	proc := processor.New(processor.Config{
		Storage:       minioStorage,
		Publisher:     publisher,
		FilesAPIURL:   cfg.FilesAPIURL,
		ThumbnailSize: cfg.ThumbnailSize,
	})

	// Create consumer
	cons, err := consumer.New(ctx, consumer.Config{
		RabbitMQURL: cfg.RabbitMQURL,
		Processor:   proc,
	})
	if err != nil {
		slog.Error("failed to create consumer", "error", err)
		os.Exit(1)
	}
	defer cons.Close()

	// Health check server
	healthServer := health.NewServer(cfg.HealthAddress)
	healthServer.AddCheck("consumer", cons.IsHealthy)
	healthServer.Start()

	// Register signal handler BEFORE starting goroutine to avoid race
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	// Handle shutdown signals
	go func() {
		<-sigCh

		slog.Info("shutting down...")
		cancel()

		shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer shutdownCancel()

		if err := healthServer.Stop(shutdownCtx); err != nil {
			slog.Error("failed to shutdown health server", "error", err)
		}
	}()

	// Start consuming
	slog.Info("starting images worker")
	if err := cons.Run(ctx); err != nil {
		slog.Error("consumer error", "error", err)
		os.Exit(1)
	}

	slog.Info("images worker stopped")
}
