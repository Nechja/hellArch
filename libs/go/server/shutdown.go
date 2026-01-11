package server

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"
	"time"
)

// ShutdownFunc is a function that performs cleanup during shutdown.
type ShutdownFunc func(ctx context.Context) error

// WaitForShutdown blocks until SIGINT or SIGTERM is received,
// then calls the provided shutdown functions with a timeout context.
func WaitForShutdown(timeout time.Duration, shutdownFuncs ...ShutdownFunc) {
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	<-sigCh

	slog.Info("shutting down gracefully...")

	ctx, cancel := context.WithTimeout(context.Background(), timeout)
	defer cancel()

	for _, fn := range shutdownFuncs {
		if err := fn(ctx); err != nil {
			slog.Error("shutdown error", "error", err)
		}
	}
}

// Run starts a blocking operation and handles shutdown signals.
// The run function should block until ctx is cancelled or an error occurs.
// After run returns or a signal is received, shutdown functions are called.
func Run(ctx context.Context, run func(ctx context.Context) error, timeout time.Duration, shutdownFuncs ...ShutdownFunc) error {
	ctx, cancel := context.WithCancel(ctx)
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	errCh := make(chan error, 1)
	go func() {
		errCh <- run(ctx)
	}()

	var runErr error
	select {
	case <-sigCh:
		slog.Info("received shutdown signal")
		cancel()
	case runErr = <-errCh:
		if runErr != nil {
			slog.Error("run error", "error", runErr)
		}
	}

	slog.Info("shutting down gracefully...")

	shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), timeout)
	defer shutdownCancel()

	for _, fn := range shutdownFuncs {
		if err := fn(shutdownCtx); err != nil {
			slog.Error("shutdown error", "error", err)
		}
	}

	return runErr
}
