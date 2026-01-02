package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"

	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/workers/webhooks/internal/client"
	"github.com/nechja/hellarch/workers/webhooks/internal/config"
	"github.com/nechja/hellarch/workers/webhooks/internal/consumer"
	"github.com/nechja/hellarch/workers/webhooks/internal/dispatcher"
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

	shutdownTelemetry, err := telemetry.Setup(ctx, "webhooks-worker")
	if err != nil {
		slog.Warn("failed to initialize telemetry", "error", err)
	} else {
		defer func() { _ = shutdownTelemetry(ctx) }()
	}

	eventsClient := client.NewEventsClient(cfg.EventsAPIURL)
	slog.Info("events API client initialized", "url", cfg.EventsAPIURL)

	disp := dispatcher.New(eventsClient, cfg.RequestTimeout)

	cons, err := consumer.New(ctx, cfg.RabbitMQURL, eventsClient, disp)
	if err != nil {
		return err
	}
	defer func() { _ = cons.Close() }()

	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		<-sigChan
		slog.Info("received shutdown signal")
		cancel()
	}()

	slog.Info("webhooks worker starting")
	return cons.Run(ctx)
}
