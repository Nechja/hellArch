package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
)

func (h *Handler) workersStatus(w http.ResponseWriter, r *http.Request) {
	statuses := h.mgmt.GetWorkersStatus(r.Context())

	// Calculate overall status
	overall := "healthy"
	for _, s := range statuses {
		if s.Status == "unhealthy" {
			overall = "unhealthy"
			break
		}
		if s.Status == "unknown" && overall == "healthy" {
			overall = "degraded"
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
