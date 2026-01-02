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

	// Check if service is excluded
	if h.excludeServices[serviceName] {
		h.logger.Info("service is excluded from restart", "service", serviceName)
		return nil
	}

	// Check cooldown
	if h.isInCooldown(serviceName) {
		h.logger.Info("service is in cooldown, skipping restart", "service", serviceName)
		return nil
	}

	// Check max restarts
	if h.exceedsMaxRestarts(serviceName) {
		h.logger.Warn("service exceeded max restarts in window, skipping",
			"service", serviceName,
			"max_restarts", h.maxRestarts,
		)
		return nil
	}

	// Perform restart
	h.logger.Warn("restarting unhealthy service", "service", serviceName)
	err := h.orch.RestartService(ctx, serviceName)
	if err != nil {
		h.logger.Error("failed to restart service", "service", serviceName, "error", err)
		return err
	}

	h.recordRestart(serviceName)
	h.logger.Info("service restart initiated", "service", serviceName)
	return nil
}

func (h *Handlers) HandleServiceRecovered(ctx context.Context, event infra.ServiceRecovered) error {
	h.logger.Info("service recovered",
		"service", event.ServiceName,
		"downtime_seconds", event.DowntimeSeconds,
	)
	return nil
}

func (h *Handlers) isInCooldown(serviceName string) bool {
	h.mu.RLock()
	defer h.mu.RUnlock()

	lastRestart, exists := h.lastRestart[serviceName]
	if !exists {
		return false
	}

	return time.Since(lastRestart) < h.cooldown
}

func (h *Handlers) exceedsMaxRestarts(serviceName string) bool {
	h.mu.RLock()
	defer h.mu.RUnlock()

	restarts := h.restartCounts[serviceName]
	if len(restarts) == 0 {
		return false
	}

	// Count restarts within the window
	cutoff := time.Now().Add(-h.restartWindow)
	count := 0
	for _, t := range restarts {
		if t.After(cutoff) {
			count++
		}
	}

	return count >= h.maxRestarts
}

func (h *Handlers) recordRestart(serviceName string) {
	h.mu.Lock()
	defer h.mu.Unlock()

	now := time.Now()
	h.lastRestart[serviceName] = now

	// Add to restart history and clean old entries
	cutoff := now.Add(-h.restartWindow)
	var filtered []time.Time
	for _, t := range h.restartCounts[serviceName] {
		if t.After(cutoff) {
			filtered = append(filtered, t)
		}
	}
	filtered = append(filtered, now)
	h.restartCounts[serviceName] = filtered
}
