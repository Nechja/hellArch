package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"

	"github.com/nechja/hellarch/golibs/health"
	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/workers/audit/internal/client"
	"github.com/nechja/hellarch/workers/audit/internal/config"
	"github.com/nechja/hellarch/workers/audit/internal/consumer"
)

func main() {
	if err := run(); err != nil {
		slog.Error("worker failed", "error", err)
		os.Exit(1)
	}
}

func run() error {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelDebug,
	}))
	slog.SetDefault(logger)

	cfg, err := config.Load()
	if err != nil {
		return err
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Start health server
	healthAddr := getEnvOrDefault("HEALTH_ADDR", ":8080")
	healthServer := health.NewServer(healthAddr)
	healthServer.Start()
	defer healthServer.Stop(ctx)

	shutdownTelemetry, err := telemetry.Setup(ctx, "audit-worker")
	if err != nil {
		slog.Warn("failed to initialize telemetry", "error", err)
	} else {
		defer func() { _ = shutdownTelemetry(ctx) }()
	}

	eventsClient := client.NewEventsClient(cfg.EventsAPIURL)
	slog.Info("events API client initialized", "url", cfg.EventsAPIURL)

	cons, err := consumer.New(ctx, cfg.RabbitMQURL, eventsClient)
	if err != nil {
		healthServer.SetHealthy(false)
		return err
	}
	defer func() { _ = cons.Close() }()

	// Add RabbitMQ connection check
	healthServer.AddCheck("rabbitmq", func() bool {
		return cons.IsHealthy()
	})

	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		<-sigChan
		slog.Info("received shutdown signal")
		cancel()
	}()

	slog.Info("audit worker starting", "health_addr", healthAddr)
	return cons.Run(ctx)
}

func getEnvOrDefault(key, defaultValue string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return defaultValue
}
