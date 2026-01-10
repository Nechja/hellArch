package logging

import (
	"log/slog"
	"os"
	"strings"
)

// Setup initializes the default slog logger based on LOG_LEVEL environment variable.
// Valid levels: debug, info, warn/warning, error. Defaults to info.
func Setup() *slog.Logger {
	level := ParseLevel(os.Getenv("LOG_LEVEL"))
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: level,
	}))
	slog.SetDefault(logger)
	return logger
}

// SetupWithLevel initializes the default slog logger with a specific level.
func SetupWithLevel(level slog.Level) *slog.Logger {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: level,
	}))
	slog.SetDefault(logger)
	return logger
}

// ParseLevel parses a log level string into a slog.Level.
// Valid values: debug, info, warn/warning, error. Defaults to info.
func ParseLevel(levelStr string) slog.Level {
	switch strings.ToLower(levelStr) {
	case "debug":
		return slog.LevelDebug
	case "warn", "warning":
		return slog.LevelWarn
	case "error":
		return slog.LevelError
	default:
		return slog.LevelInfo
	}
}
