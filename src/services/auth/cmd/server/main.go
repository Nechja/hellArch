package main

import (
	"context"
	"log/slog"
	"net"
	"net/http"
	"os"
	"os/signal"
	"strings"
	"syscall"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/events"
	"github.com/nechja/hellarch/auth/internal/handler"
	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/store"
	"github.com/nechja/hellarch/auth/internal/telemetry"
	"go.opentelemetry.io/contrib/instrumentation/google.golang.org/grpc/otelgrpc"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

func main() {
	logLevel := slog.LevelDebug // Default to debug for dev
	switch strings.ToLower(os.Getenv("LOG_LEVEL")) {
	case "info":
		logLevel = slog.LevelInfo
	case "warn", "warning":
		logLevel = slog.LevelWarn
	case "error":
		logLevel = slog.LevelError
	}

	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: logLevel,
	}))
	slog.SetDefault(logger)

	cfg, err := config.Load()
	if err != nil {
		slog.Error("failed to load config", "error", err)
		os.Exit(1)
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Initialize OpenTelemetry
	shutdownTelemetry, err := telemetry.Setup(ctx, "auth")
	if err != nil {
		slog.Warn("failed to initialize telemetry, continuing without tracing", "error", err)
	} else {
		defer shutdownTelemetry(ctx)
	}

	// Initialize stores
	pgStore, err := store.NewPostgres(ctx, cfg.DatabaseURL)
	if err != nil {
		slog.Error("failed to connect to postgres", "error", err)
		os.Exit(1)
	}
	defer pgStore.Close()

	redisStore, err := store.NewRedis(ctx, cfg.RedisURL)
	if err != nil {
		slog.Error("failed to connect to redis", "error", err)
		os.Exit(1)
	}
	defer redisStore.Close()

	// Initialize event publisher
	eventPublisher, err := events.NewPublisher(ctx, cfg.RabbitMQURL, "hellarch/auth")
	if err != nil {
		slog.Error("failed to connect to RabbitMQ", "error", err)
		os.Exit(1)
	}
	defer eventPublisher.Close()

	// Initialize services
	jwtService, err := jwt.NewService(cfg.JWTPrivateKeyPath, cfg.JWTPublicKeyPath)
	if err != nil {
		slog.Error("failed to initialize jwt service", "error", err)
		os.Exit(1)
	}

	oauthService := oauth.NewService(cfg.OAuth)

	// Initialize handlers
	authHandler := handler.NewAuthHandler(pgStore, redisStore, jwtService, oauthService, eventPublisher, cfg)
	httpHandler := handler.NewHTTPHandler(authHandler)

	// Initialize gRPC server with OTel instrumentation
	grpcServer := grpc.NewServer(
		grpc.StatsHandler(otelgrpc.NewServerHandler()),
	)
	handler.RegisterAuthServiceServer(grpcServer, authHandler)

	if cfg.Environment == "development" {
		reflection.Register(grpcServer)
	}

	// Initialize HTTP server with OTel instrumentation
	mux := http.NewServeMux()
	httpHandler.RegisterRoutes(mux)
	httpServer := &http.Server{
		Addr:    cfg.HTTPAddress,
		Handler: otelhttp.NewHandler(mux, "auth-http"),
	}

	// Start gRPC server
	grpcLis, err := net.Listen("tcp", cfg.GRPCAddress)
	if err != nil {
		slog.Error("failed to listen for grpc", "error", err, "address", cfg.GRPCAddress)
		os.Exit(1)
	}

	go func() {
		slog.Info("gRPC server starting", "address", cfg.GRPCAddress)
		if err := grpcServer.Serve(grpcLis); err != nil {
			slog.Error("gRPC server failed", "error", err)
		}
	}()

	// Start HTTP server
	go func() {
		slog.Info("HTTP server starting", "address", cfg.HTTPAddress)
		if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			slog.Error("HTTP server failed", "error", err)
		}
	}()

	// Graceful shutdown
	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)
	<-sigChan

	slog.Info("shutting down gracefully...")
	grpcServer.GracefulStop()
	if err := httpServer.Shutdown(ctx); err != nil {
		slog.Error("HTTP server shutdown error", "error", err)
	}
	cancel()
}
