package monitor

import (
	"context"
	"log/slog"
	"sync"
	"time"

	"github.com/nechja/hellarch/golibs/rabbitmq"
)

// WorkerMonitor watches RabbitMQ queues and emits events when workers go unhealthy
type WorkerMonitor struct {
	mgmt       *rabbitmq.ManagementClient
	publisher  *rabbitmq.Publisher
	interval   time.Duration
	prevStatus map[string]string // queue name -> previous status
	mu         sync.Mutex
}

// New creates a new WorkerMonitor
func New(mgmt *rabbitmq.ManagementClient, publisher *rabbitmq.Publisher, interval time.Duration) *WorkerMonitor {
	return &WorkerMonitor{
		mgmt:       mgmt,
		publisher:  publisher,
		interval:   interval,
		prevStatus: make(map[string]string),
	}
}

// Start begins monitoring workers in the background
func (m *WorkerMonitor) Start(ctx context.Context) {
	slog.Info("starting worker monitor", "interval", m.interval)

	ticker := time.NewTicker(m.interval)
	defer ticker.Stop()

	// Initial check
	m.checkWorkers(ctx)

	for {
		select {
		case <-ctx.Done():
			slog.Info("worker monitor stopped")
			return
		case <-ticker.C:
			m.checkWorkers(ctx)
		}
	}
}

func (m *WorkerMonitor) checkWorkers(ctx context.Context) {
	statuses := m.mgmt.GetWorkersStatus(ctx)

	m.mu.Lock()
	defer m.mu.Unlock()

	for _, worker := range statuses {
		prevStatus, exists := m.prevStatus[worker.Name]

		// Check for transitions
		if exists && prevStatus != worker.Status {
			m.handleTransition(ctx, worker, prevStatus)
		} else if !exists && worker.Status == "unhealthy" {
			// First time seeing this worker and it's already unhealthy
			m.emitUnhealthy(ctx, worker)
		}

		m.prevStatus[worker.Name] = worker.Status
	}
}

func (m *WorkerMonitor) handleTransition(ctx context.Context, worker rabbitmq.WorkerStatus, prevStatus string) {
	switch {
	case prevStatus == "healthy" && worker.Status == "unhealthy":
		m.emitUnhealthy(ctx, worker)
	case prevStatus == "unhealthy" && worker.Status == "healthy":
		m.emitRecovered(ctx, worker)
	}
}

func (m *WorkerMonitor) emitUnhealthy(ctx context.Context, worker rabbitmq.WorkerStatus) {
	slog.Warn("worker became unhealthy",
		"worker", worker.Name,
		"queue", worker.Queue,
		"consumers", worker.Consumers,
	)

	if m.publisher == nil {
		slog.Warn("no publisher configured, skipping event emission")
		return
	}

	data := map[string]any{
		"service_name":         worker.Name,
		"status":               "unhealthy",
		"consecutive_failures": 1,
		"last_error":           "No consumers on queue",
		"detected_at":          time.Now().UTC().Format(time.RFC3339),
	}

	if err := m.publisher.Publish(ctx, "hellarch.infra.service.unhealthy.v1", data); err != nil {
		slog.Error("failed to publish worker unhealthy event", "error", err, "worker", worker.Name)
	}
}

func (m *WorkerMonitor) emitRecovered(ctx context.Context, worker rabbitmq.WorkerStatus) {
	slog.Info("worker recovered",
		"worker", worker.Name,
		"queue", worker.Queue,
		"consumers", worker.Consumers,
	)

	if m.publisher == nil {
		slog.Warn("no publisher configured, skipping event emission")
		return
	}

	data := map[string]any{
		"service_name":     worker.Name,
		"recovered_at":     time.Now().UTC().Format(time.RFC3339),
		"downtime_seconds": 0, // We don't track exact downtime
	}

	if err := m.publisher.Publish(ctx, "hellarch.infra.service.recovered.v1", data); err != nil {
		slog.Error("failed to publish worker recovered event", "error", err, "worker", worker.Name)
	}
}
