package handlers

import (
	"context"
	"log/slog"
	"sync"
	"time"

	"github.com/nechja/hellarch/cloudevents/infra"
	"github.com/nechja/hellarch/infra-worker/internal/orchestrator"
)

type Handlers struct {
	orch     orchestrator.Orchestrator
	logger   *slog.Logger
	cooldown time.Duration
	maxRestarts int
	restartWindow time.Duration

	mu             sync.RWMutex
	lastRestart    map[string]time.Time
	restartCounts  map[string][]time.Time
	excludeServices map[string]bool
}

type Config struct {
	CooldownSeconds   int
	MaxRestarts       int
	RestartWindowSecs int
	ExcludeServices   []string
}

func New(orch orchestrator.Orchestrator, cfg Config) *Handlers {
	exclude := make(map[string]bool)
	for _, svc := range cfg.ExcludeServices {
		exclude[svc] = true
	}

	// Always exclude self and health service
	exclude["infra-worker"] = true
	exclude["health"] = true

	return &Handlers{
		orch:            orch,
		logger:          slog.Default().With("component", "handlers"),
		cooldown:        time.Duration(cfg.CooldownSeconds) * time.Second,
		maxRestarts:     cfg.MaxRestarts,
		restartWindow:   time.Duration(cfg.RestartWindowSecs) * time.Second,
		lastRestart:     make(map[string]time.Time),
		restartCounts:   make(map[string][]time.Time),
		excludeServices: exclude,
	}
}

func (h *Handlers) HandleServiceUnhealthy(ctx context.Context, event infra.ServiceUnhealthy) error {
	serviceName := event.ServiceName

	h.logger.Info("received service unhealthy event",
		"service", serviceName,
		"status", event.Status,
		"consecutive_failures", event.ConsecutiveFailures,
	)

	// Check if service is excluded (immutable, no lock needed)
	if h.excludeServices[serviceName] {
		h.logger.Info("service is excluded from restart", "service", serviceName)
		return nil
	}

	// Atomically check conditions and record restart intent
	shouldRestart := h.tryAcquireRestartPermission(serviceName)
	if !shouldRestart {
		return nil
	}

	// Perform restart (outside lock to avoid holding lock during I/O)
	h.logger.Warn("restarting unhealthy service", "service", serviceName)
	err := h.orch.RestartService(ctx, serviceName)
	if err != nil {
		h.logger.Error("failed to restart service", "service", serviceName, "error", err)
		return err
	}

	h.logger.Info("service restart initiated", "service", serviceName)
	return nil
}

func (h *Handlers) tryAcquireRestartPermission(serviceName string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	now := time.Now()

	// Check cooldown
	if lastRestart, exists := h.lastRestart[serviceName]; exists {
		if now.Sub(lastRestart) < h.cooldown {
			h.logger.Info("service is in cooldown, skipping restart", "service", serviceName)
			return false
		}
	}

	// Check max restarts - count restarts within the window
	cutoff := now.Add(-h.restartWindow)
	count := 0
	var filtered []time.Time
	for _, t := range h.restartCounts[serviceName] {
		if t.After(cutoff) {
			count++
			filtered = append(filtered, t)
		}
	}

	if count >= h.maxRestarts {
		h.logger.Warn("service exceeded max restarts in window, skipping",
			"service", serviceName,
			"max_restarts", h.maxRestarts,
		)
		return false
	}

	// Record restart intent atomically with the check
	h.lastRestart[serviceName] = now
	filtered = append(filtered, now)
	h.restartCounts[serviceName] = filtered

	return true
}

func (h *Handlers) HandleServiceRecovered(ctx context.Context, event infra.ServiceRecovered) error {
	h.logger.Info("service recovered",
		"service", event.ServiceName,
		"downtime_seconds", event.DowntimeSeconds,
	)
	return nil
}
