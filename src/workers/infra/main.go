package main

import (
	"context"
	"encoding/json"
	"log/slog"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"syscall"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/infra"
	"github.com/nechja/hellarch/golibs/health"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/infra-worker/internal/handlers"
	"github.com/nechja/hellarch/infra-worker/internal/orchestrator"
)

func main() {
	// Configure structured logging
	logLevel := slog.LevelInfo
	if os.Getenv("LOG_LEVEL") == "debug" {
		logLevel = slog.LevelDebug
	}
	slog.SetDefault(slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{Level: logLevel})))

	logger := slog.Default()
	logger.Info("starting infra-worker")

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Start health server
	healthAddr := getEnvOrDefault("HEALTH_ADDR", ":8080")
	healthServer := health.NewServer(healthAddr)
	healthServer.Start()
	defer healthServer.Stop(ctx)

	// Setup OpenTelemetry
	serviceName := getEnvOrDefault("OTEL_SERVICE_NAME", "infra-worker")
	shutdown, err := telemetry.Setup(ctx, serviceName)
	if err != nil {
		logger.Error("failed to setup otel", "error", err)
		os.Exit(1)
	}
	defer shutdown(ctx)

	// Create orchestrator
	backend := getEnvOrDefault("ORCHESTRATOR_BACKEND", "docker")
	orch, err := orchestrator.New(backend, orchestrator.Options{Logger: logger})
	if err != nil {
		logger.Error("failed to create orchestrator", "backend", backend, "error", err)
		healthServer.SetHealthy(false)
		os.Exit(1)
	}
	defer orch.Close()
	logger.Info("orchestrator initialized", "backend", backend)

	// Create handlers
	handlersConfig := handlers.Config{
		CooldownSeconds:   getEnvIntOrDefault("RESTART_COOLDOWN_SECONDS", 60),
		MaxRestarts:       getEnvIntOrDefault("MAX_RESTARTS", 3),
		RestartWindowSecs: getEnvIntOrDefault("RESTART_WINDOW_SECONDS", 300),
		ExcludeServices:   strings.Split(getEnvOrDefault("EXCLUDE_SERVICES", ""), ","),
	}
	h := handlers.New(orch, logger, handlersConfig)

	// Create RabbitMQ consumer
	rabbitmqURL := getEnvOrDefault("RABBITMQ_URL", "amqp://hellarch:hellarch@localhost:5672")
	consumer, err := rabbitmq.NewConsumer(ctx, rabbitmq.ConsumerConfig{
		URL:        rabbitmqURL,
		QueueName:  "infra-events",
		RoutingKey: "hellarch.infra.#",
		TracerName: "infra-worker",
	})
	if err != nil {
		logger.Error("failed to create rabbitmq consumer", "error", err)
		healthServer.SetHealthy(false)
		os.Exit(1)
	}
	defer consumer.Close()

	// Add health checks
	healthServer.AddCheck("rabbitmq", func() bool {
		return consumer.IsHealthy()
	})
	healthServer.AddCheck("orchestrator", func() bool {
		return orch != nil
	})

	// Setup graceful shutdown
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		<-sigCh
		logger.Info("received shutdown signal")
		cancel()
	}()

	// Start consuming
	logger.Info("infra-worker ready, consuming events", "health_addr", healthAddr)
	if err := consumer.Run(ctx, createMessageHandler(h, logger)); err != nil {
		if ctx.Err() != nil {
			logger.Info("consumer stopped due to context cancellation")
		} else {
			logger.Error("consumer error", "error", err)
			os.Exit(1)
		}
	}

	logger.Info("infra-worker shutdown complete")
}

func createMessageHandler(h *handlers.Handlers, logger *slog.Logger) rabbitmq.MessageHandler {
	return func(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error {
		switch event.Type {
		case "hellarch.infra.service.unhealthy.v1":
			var data infra.ServiceUnhealthy
			if err := json.Unmarshal(event.Data, &data); err != nil {
				logger.Error("failed to unmarshal ServiceUnhealthy", "error", err)
				return err
			}
			return h.HandleServiceUnhealthy(ctx, data)

		case "hellarch.infra.service.recovered.v1":
			var data infra.ServiceRecovered
			if err := json.Unmarshal(event.Data, &data); err != nil {
				logger.Error("failed to unmarshal ServiceRecovered", "error", err)
				return err
			}
			return h.HandleServiceRecovered(ctx, data)

		default:
			logger.Debug("ignoring unhandled event type", "type", event.Type)
			return nil
		}
	}
}

func getEnvOrDefault(key, defaultValue string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return defaultValue
}

func getEnvIntOrDefault(key string, defaultValue int) int {
	if v := os.Getenv(key); v != "" {
		if i, err := strconv.Atoi(v); err == nil {
			return i
		}
	}
	return defaultValue
}
