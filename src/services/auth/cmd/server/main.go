package main

import (
	"context"
	"log/slog"
	"net"
	"net/http"
	"os"
	"os/signal"
	"syscall"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/handler"
	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelDebug,
	}))
	slog.SetDefault(logger)

	cfg, err := config.Load()
	if err != nil {
		slog.Error("failed to load config", "error", err)
		os.Exit(1)
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

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

	// Initialize services
	jwtService, err := jwt.NewService(cfg.JWTPrivateKeyPath, cfg.JWTPublicKeyPath)
	if err != nil {
		slog.Error("failed to initialize jwt service", "error", err)
		os.Exit(1)
	}

	oauthService := oauth.NewService(cfg.OAuth)

	// Initialize handlers
	authHandler := handler.NewAuthHandler(pgStore, redisStore, jwtService, oauthService, cfg)
	httpHandler := handler.NewHTTPHandler(authHandler)

	// Initialize gRPC server
	grpcServer := grpc.NewServer()
	handler.RegisterAuthServiceServer(grpcServer, authHandler)

	if cfg.Environment == "development" {
		reflection.Register(grpcServer)
	}

	// Initialize HTTP server
	mux := http.NewServeMux()
	httpHandler.RegisterRoutes(mux)
	httpServer := &http.Server{
		Addr:    cfg.HTTPAddress,
		Handler: mux,
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
