package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"

	"github.com/nechja/hellarch/workers/audit/internal/config"
	"github.com/nechja/hellarch/workers/audit/internal/consumer"
	"github.com/nechja/hellarch/workers/audit/internal/store"
	"github.com/nechja/hellarch/workers/audit/internal/telemetry"
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

	shutdownTelemetry, err := telemetry.Setup(ctx, cfg.OTLPEndpoint)
	if err != nil {
		slog.Warn("failed to initialize telemetry", "error", err)
	} else {
		defer func() { _ = shutdownTelemetry(ctx) }()
	}

	pgStore, err := store.NewPostgres(ctx, cfg.DatabaseURL)
	if err != nil {
		return err
	}
	defer pgStore.Close()

	cons, err := consumer.New(ctx, cfg.RabbitMQURL, pgStore)
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

	slog.Info("audit worker starting")
	return cons.Run(ctx)
}
