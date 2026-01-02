package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
)

// Known workers and their queue names
var knownWorkers = map[string]string{
	"audit-worker":    "audit-worker",
	"webhooks-worker": "webhooks-worker",
	"infra-worker":    "infra-events",
	"dlq-worker":      "dlq-events",
}

func (h *Handler) workersStatus(w http.ResponseWriter, r *http.Request) {
	statuses := h.mgmt.GetWorkersStatus(r.Context(), knownWorkers)

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
