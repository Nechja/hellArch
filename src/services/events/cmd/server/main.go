package main

import (
	"context"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/services/events/internal/config"
	"github.com/nechja/hellarch/services/events/internal/handler"
	"github.com/nechja/hellarch/services/events/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
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
	cfg, err := config.Load()
	if err != nil {
		slog.Error("failed to load config", "error", err)
		os.Exit(1)
	}

	// Setup telemetry
	shutdown, err := telemetry.Setup(ctx, "events")
	if err != nil {
		slog.Warn("failed to setup telemetry", "error", err)
	} else {
		defer shutdown(ctx)
	}

	// Connect to database
	st, err := store.New(ctx, cfg.DatabaseURL)
	if err != nil {
		slog.Error("failed to connect to database", "error", err)
		os.Exit(1)
	}
	defer st.Close()
	slog.Info("connected to database")

	// Connect to RabbitMQ for publishing
	var publisher *rabbitmq.Publisher
	if cfg.RabbitMQURL != "" {
		publisher, err = rabbitmq.NewPublisher(ctx, rabbitmq.PublisherConfig{
			URL:        cfg.RabbitMQURL,
			Source:     "hellarch/events",
			TracerName: "events/publisher",
		})
		if err != nil {
			slog.Warn("failed to connect to RabbitMQ", "error", err)
		} else {
			defer publisher.Close()
			slog.Info("connected to RabbitMQ")
		}
	}

	// Setup HTTP server
	h := handler.New(st, publisher)
	mux := http.NewServeMux()
	h.RegisterRoutes(mux)

	server := &http.Server{
		Addr:    cfg.HTTPAddress,
		Handler: otelhttp.NewHandler(mux, "events-api"),
	}

	// Graceful shutdown
	go func() {
		sigCh := make(chan os.Signal, 1)
		signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
		<-sigCh

		slog.Info("shutting down...")
		cancel()

		shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer shutdownCancel()

		if err := server.Shutdown(shutdownCtx); err != nil {
			slog.Error("failed to shutdown server", "error", err)
		}
	}()

	slog.Info("starting server", "address", cfg.HTTPAddress)
	if err := server.ListenAndServe(); err != http.ErrServerClosed {
		slog.Error("server error", "error", err)
		os.Exit(1)
	}

	slog.Info("server stopped")
}
