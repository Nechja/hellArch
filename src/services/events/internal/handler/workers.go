package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
)

const (
	statusHealthy   = "healthy"
	statusUnhealthy = "unhealthy"
	statusDegraded  = "degraded"
	statusUnknown   = "unknown"
)

func (h *Handler) workersStatus(w http.ResponseWriter, r *http.Request) {
	statuses := h.mgmt.GetWorkersStatus(r.Context())

	// Calculate overall status
	overall := statusHealthy
	for _, s := range statuses {
		if s.Status == statusUnhealthy {
			overall = statusUnhealthy
			break
		}
		if s.Status == statusUnknown && overall == statusHealthy {
			overall = statusDegraded
		}
	}

	response := struct {
		Overall string                   `json:"overall"`
		Workers []rabbitmq.WorkerStatus `json:"workers"`
	}{
		Overall: overall,
		Workers: statuses,
	}

	writeJSON(w, http.StatusOK, response)
}
