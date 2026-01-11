package main

import (
	"context"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/nechja/hellarch/golibs/logging"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/telemetry"
	"github.com/nechja/hellarch/services/files/internal/config"
	"github.com/nechja/hellarch/services/files/internal/handler"
	"github.com/nechja/hellarch/services/files/internal/service"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	logging.Setup()

	// Load config
	cfg, err := config.Load()
	if err != nil {
		slog.Error("failed to load config", "error", err)
		os.Exit(1)
	}

	// Setup telemetry
	shutdown, err := telemetry.Setup(ctx, "files")
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
	if cfg.RabbitMQURL != "" {
		publisher, err = rabbitmq.NewPublisher(ctx, rabbitmq.PublisherConfig{
			URL:        cfg.RabbitMQURL,
			Source:     "hellarch/files",
			TracerName: "files/publisher",
		})
		if err != nil {
			slog.Warn("failed to connect to RabbitMQ", "error", err)
		} else {
			defer publisher.Close()
			slog.Info("connected to RabbitMQ")
		}
	}

	// Create services
	uploadService := service.NewUpload(st, minioStorage, publisher, cfg.MaxUploadSize)
	fileService := service.NewFile(st, minioStorage, publisher)
	variantService := service.NewVariant(st, minioStorage)

	// Setup HTTP server
	h := handler.New(uploadService, fileService, variantService)
	mux := http.NewServeMux()
	h.RegisterRoutes(mux)

	server := &http.Server{
		Addr:    cfg.HTTPAddress,
		Handler: otelhttp.NewHandler(mux, "files-api"),
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
