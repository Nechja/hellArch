package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"syscall"

	"github.com/nechja/hellarch/infra-worker/internal/handlers"
	"github.com/nechja/hellarch/infra-worker/internal/orchestrator"
	"github.com/nechja/hellarch/infra-worker/internal/rabbitmq"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracehttp"
	"go.opentelemetry.io/otel/propagation"
	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
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

	// Setup OpenTelemetry
	shutdown, err := setupOtel()
	if err != nil {
		logger.Error("failed to setup otel", "error", err)
		os.Exit(1)
	}
	defer shutdown()

	// Create orchestrator
	backend := getEnvOrDefault("ORCHESTRATOR_BACKEND", "docker")
	orch, err := orchestrator.New(backend)
	if err != nil {
		logger.Error("failed to create orchestrator", "backend", backend, "error", err)
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
	h := handlers.New(orch, handlersConfig)

	// Create RabbitMQ consumer
	rabbitmqURL := getEnvOrDefault("RABBITMQ_URL", "amqp://hellarch:hellarch@localhost:5672")
	consumer, err := rabbitmq.NewConsumer(rabbitmqURL, h)
	if err != nil {
		logger.Error("failed to create rabbitmq consumer", "error", err)
		os.Exit(1)
	}
	defer consumer.Close()

	// Setup graceful shutdown
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		<-sigCh
		logger.Info("received shutdown signal")
		cancel()
	}()

	// Start consuming
	logger.Info("infra-worker ready, consuming events")
	if err := consumer.Start(ctx); err != nil {
		logger.Error("consumer error", "error", err)
		os.Exit(1)
	}

	logger.Info("infra-worker shutdown complete")
}

func setupOtel() (func(), error) {
	ctx := context.Background()

	endpoint := getEnvOrDefault("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")
	serviceName := getEnvOrDefault("OTEL_SERVICE_NAME", "infra-worker")

	exporter, err := otlptracehttp.New(ctx,
		otlptracehttp.WithEndpoint(strings.TrimPrefix(strings.TrimPrefix(endpoint, "http://"), "https://")),
		otlptracehttp.WithInsecure(),
	)
	if err != nil {
		return nil, err
	}

	res, err := resource.New(ctx,
		resource.WithAttributes(semconv.ServiceName(serviceName)),
	)
	if err != nil {
		return nil, err
	}

	tp := sdktrace.NewTracerProvider(
		sdktrace.WithBatcher(exporter),
		sdktrace.WithResource(res),
	)

	otel.SetTracerProvider(tp)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(
		propagation.TraceContext{},
		propagation.Baggage{},
	))

	return func() {
		tp.Shutdown(context.Background())
	}, nil
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
