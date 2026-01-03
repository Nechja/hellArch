package health

import (
	"context"
	"encoding/json"
	"log/slog"
	"net/http"
	"sync/atomic"
	"time"
)

// Status represents the health status of a service.
type Status struct {
	Status    string            `json:"status"`
	Timestamp string            `json:"timestamp"`
	Checks    map[string]string `json:"checks,omitempty"`
}

// Server provides a health check HTTP endpoint.
type Server struct {
	server  *http.Server
	healthy atomic.Bool
	checks  map[string]func() bool
}

// NewServer creates a new health server listening on the specified address.
func NewServer(addr string) *Server {
	s := &Server{
		checks: make(map[string]func() bool),
	}
	s.healthy.Store(true)

	mux := http.NewServeMux()
	mux.HandleFunc("/health", s.handleHealth)
	mux.HandleFunc("/ready", s.handleReady)

	s.server = &http.Server{
		Addr:              addr,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}

	return s
}

// AddCheck registers a named health check function.
func (s *Server) AddCheck(name string, check func() bool) {
	s.checks[name] = check
}

// SetHealthy sets the overall health status.
func (s *Server) SetHealthy(healthy bool) {
	s.healthy.Store(healthy)
}

// Start starts the health server in a goroutine.
func (s *Server) Start() {
	go func() {
		slog.Info("health server starting", "addr", s.server.Addr)
		if err := s.server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			slog.Error("health server error", "error", err)
		}
	}()
}

// Stop gracefully stops the health server.
func (s *Server) Stop(ctx context.Context) error {
	return s.server.Shutdown(ctx)
}

func (s *Server) handleHealth(w http.ResponseWriter, r *http.Request) {
	status := Status{
		Timestamp: time.Now().UTC().Format(time.RFC3339),
		Checks:    make(map[string]string),
	}

	allHealthy := s.healthy.Load()

	for name, check := range s.checks {
		if check() {
			status.Checks[name] = "healthy"
		} else {
			status.Checks[name] = "unhealthy"
			allHealthy = false
		}
	}

	if allHealthy {
		status.Status = "healthy"
		w.WriteHeader(http.StatusOK)
	} else {
		status.Status = "unhealthy"
		w.WriteHeader(http.StatusServiceUnavailable)
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(status)
}

func (s *Server) handleReady(w http.ResponseWriter, r *http.Request) {
	if s.healthy.Load() {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte(`{"status":"ready"}`))
	} else {
		w.WriteHeader(http.StatusServiceUnavailable)
		w.Write([]byte(`{"status":"not ready"}`))
	}
}
