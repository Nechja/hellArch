package main

import (
	"context"
	"errors"
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
	if err := run(); err != nil {
		slog.Error("server failed", "error", err)
		os.Exit(1)
	}
}

func run() error {
	logLevel := slog.LevelDebug
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
		return errors.New("failed to load config: " + err.Error())
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	shutdownTelemetry, err := telemetry.Setup(ctx, "auth")
	if err != nil {
		slog.Warn("failed to initialize telemetry, continuing without tracing", "error", err)
	} else {
		defer func() { _ = shutdownTelemetry(ctx) }()
	}

	pgStore, err := store.NewPostgres(ctx, cfg.DatabaseURL)
	if err != nil {
		return errors.New("failed to connect to postgres: " + err.Error())
	}
	defer pgStore.Close()

	redisStore, err := store.NewRedis(ctx, cfg.RedisURL)
	if err != nil {
		return errors.New("failed to connect to redis: " + err.Error())
	}
	defer func() { _ = redisStore.Close() }()

	eventPublisher, err := events.NewPublisher(ctx, cfg.RabbitMQURL, "hellarch/auth")
	if err != nil {
		return errors.New("failed to connect to RabbitMQ: " + err.Error())
	}
	defer func() { _ = eventPublisher.Close() }()

	jwtService, err := jwt.NewService(cfg.JWTPrivateKeyPath, cfg.JWTPublicKeyPath)
	if err != nil {
		return errors.New("failed to initialize jwt service: " + err.Error())
	}

	oauthService := oauth.NewService(cfg.OAuth)

	authHandler := handler.NewAuthHandler(pgStore, redisStore, jwtService, oauthService, eventPublisher, cfg)
	httpHandler := handler.NewHTTPHandler(authHandler, cfg)
	adminHandler := handler.NewAdminHandler(pgStore, redisStore)

	grpcServer := grpc.NewServer(
		grpc.StatsHandler(otelgrpc.NewServerHandler()),
	)
	handler.RegisterAuthServiceServer(grpcServer, authHandler)

	if cfg.Environment == "development" {
		reflection.Register(grpcServer)
	}

	mux := http.NewServeMux()
	httpHandler.RegisterRoutes(mux)
	adminHandler.RegisterRoutes(mux)
	httpServer := &http.Server{
		Addr:    cfg.HTTPAddress,
		Handler: otelhttp.NewHandler(mux, "auth-http"),
	}

	grpcLis, err := net.Listen("tcp", cfg.GRPCAddress)
	if err != nil {
		return errors.New("failed to listen for grpc: " + err.Error())
	}

	go func() {
		slog.Info("gRPC server starting", "address", cfg.GRPCAddress)
		if err := grpcServer.Serve(grpcLis); err != nil {
			slog.Error("gRPC server failed", "error", err)
		}
	}()

	go func() {
		slog.Info("HTTP server starting", "address", cfg.HTTPAddress)
		if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			slog.Error("HTTP server failed", "error", err)
		}
	}()

	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)
	<-sigChan

	slog.Info("shutting down gracefully...")
	grpcServer.GracefulStop()
	if err := httpServer.Shutdown(ctx); err != nil {
		slog.Error("HTTP server shutdown error", "error", err)
	}

	return nil
}
