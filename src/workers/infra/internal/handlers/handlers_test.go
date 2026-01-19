package handlers

import (
	"context"
	"errors"
	"sync"
	"testing"
	"time"

	"github.com/nechja/hellarch/cloudevents/infra"
)

// mockOrchestrator implements orchestrator.Orchestrator for testing
type mockOrchestrator struct {
	restartCalls []string
	mu           sync.Mutex
	shouldFail   bool
}

func (m *mockOrchestrator) RestartService(_ context.Context, serviceName string) error {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.restartCalls = append(m.restartCalls, serviceName)
	if m.shouldFail {
		return errors.New("restart failed")
	}
	return nil
}

func (m *mockOrchestrator) Close() error { return nil }

func (m *mockOrchestrator) getRestartCalls() []string {
	m.mu.Lock()
	defer m.mu.Unlock()
	result := make([]string, len(m.restartCalls))
	copy(result, m.restartCalls)
	return result
}

func TestExcludedServicesNotRestarted(t *testing.T) {
	mock := &mockOrchestrator{}
	h := New(mock, nil, Config{
		CooldownSeconds:   0,
		MaxRestarts:       10,
		RestartWindowSecs: 60,
		ExcludeServices:   []string{"custom-excluded"},
	})

	tests := []struct {
		name    string
		service string
		want    bool // true = should restart
	}{
		{"infra-worker always excluded", "infra-worker", false},
		{"health always excluded", "health", false},
		{"custom excluded", "custom-excluded", false},
		{"normal service restarts", "api-gateway", true},
		{"another normal service", "user-service", true},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			mock.restartCalls = nil

			err := h.HandleServiceUnhealthy(context.Background(), infra.ServiceUnhealthy{
				ServiceName: tt.service,
			})

			if err != nil {
				t.Fatalf("unexpected error: %v", err)
			}

			calls := mock.getRestartCalls()
			got := len(calls) > 0

			if got != tt.want {
				t.Errorf("service %q: got restart=%v, want restart=%v", tt.service, got, tt.want)
			}
		})
	}
}

func TestCooldownPreventsRapidRestarts(t *testing.T) {
	mock := &mockOrchestrator{}
	h := New(mock, nil, Config{
		CooldownSeconds:   1, // 1 second cooldown
		MaxRestarts:       10,
		RestartWindowSecs: 60,
	})

	event := infra.ServiceUnhealthy{ServiceName: "test-service"}

	// First restart should work
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 1 {
		t.Fatal("first restart should succeed")
	}

	// Immediate second restart should be blocked by cooldown
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 1 {
		t.Error("second restart should be blocked by cooldown")
	}

	// Wait for cooldown to expire
	time.Sleep(1100 * time.Millisecond)

	// Now restart should work again
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 2 {
		t.Error("restart after cooldown should succeed")
	}
}

func TestMaxRestartsInWindowEnforced(t *testing.T) {
	mock := &mockOrchestrator{}
	h := New(mock, nil, Config{
		CooldownSeconds:   0, // No cooldown for this test
		MaxRestarts:       3,
		RestartWindowSecs: 60,
	})

	event := infra.ServiceUnhealthy{ServiceName: "flaky-service"}

	// Should allow exactly MaxRestarts
	for i := 0; i < 3; i++ {
		h.HandleServiceUnhealthy(context.Background(), event)
	}

	if len(mock.getRestartCalls()) != 3 {
		t.Fatalf("expected 3 restarts, got %d", len(mock.getRestartCalls()))
	}

	// 4th restart should be blocked
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 3 {
		t.Error("4th restart should be blocked by max restarts limit")
	}
}

func TestRestartWindowExpiry(t *testing.T) {
	mock := &mockOrchestrator{}
	h := New(mock, nil, Config{
		CooldownSeconds:   0,
		MaxRestarts:       2,
		RestartWindowSecs: 1, // 1 second window
	})

	event := infra.ServiceUnhealthy{ServiceName: "test-service"}

	// Use up all restarts
	h.HandleServiceUnhealthy(context.Background(), event)
	h.HandleServiceUnhealthy(context.Background(), event)

	if len(mock.getRestartCalls()) != 2 {
		t.Fatal("expected 2 restarts")
	}

	// Should be blocked
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 2 {
		t.Error("should be blocked after max restarts")
	}

	// Wait for window to expire
	time.Sleep(1100 * time.Millisecond)

	// Now should be allowed again
	h.HandleServiceUnhealthy(context.Background(), event)
	if len(mock.getRestartCalls()) != 3 {
		t.Error("restart should be allowed after window expires")
	}
}

func TestDifferentServicesTrackedSeparately(t *testing.T) {
	mock := &mockOrchestrator{}
	h := New(mock, nil, Config{
		CooldownSeconds:   0,
		MaxRestarts:       1,
		RestartWindowSecs: 60,
	})

	// Max out service A
	h.HandleServiceUnhealthy(context.Background(), infra.ServiceUnhealthy{ServiceName: "service-a"})
	h.HandleServiceUnhealthy(context.Background(), infra.ServiceUnhealthy{ServiceName: "service-a"})

	// Service B should still be able to restart
	h.HandleServiceUnhealthy(context.Background(), infra.ServiceUnhealthy{ServiceName: "service-b"})

	calls := mock.getRestartCalls()
	if len(calls) != 2 {
		t.Fatalf("expected 2 total restarts, got %d", len(calls))
	}

	// Verify it's one of each
	aCount, bCount := 0, 0
	for _, c := range calls {
		if c == "service-a" {
			aCount++
		} else if c == "service-b" {
			bCount++
		}
	}

	if aCount != 1 || bCount != 1 {
		t.Errorf("expected 1 restart each, got service-a=%d, service-b=%d", aCount, bCount)
	}
}

func TestOrchestratorErrorPropagated(t *testing.T) {
	mock := &mockOrchestrator{shouldFail: true}
	h := New(mock, nil, Config{
		CooldownSeconds:   0,
		MaxRestarts:       10,
		RestartWindowSecs: 60,
	})

	err := h.HandleServiceUnhealthy(context.Background(), infra.ServiceUnhealthy{
		ServiceName: "failing-service",
	})

	if err == nil {
		t.Error("expected error from failed orchestrator")
	}
}
