package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/status"
)

type WorkerHandler struct {
	mgmt *rabbitmq.ManagementClient
}

func NewWorkerHandler(mgmt *rabbitmq.ManagementClient) *WorkerHandler {
	return &WorkerHandler{mgmt: mgmt}
}

func (h *WorkerHandler) workersStatus(w http.ResponseWriter, r *http.Request) {
	statuses := h.mgmt.GetWorkersStatus(r.Context())

	// Calculate overall status
	overall := status.Healthy
	for _, s := range statuses {
		if s.Status == status.Unhealthy {
			overall = status.Unhealthy
			break
		}
		if s.Status == status.Unknown && overall == status.Healthy {
			overall = status.Degraded
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
