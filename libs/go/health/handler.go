package health

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/httputil"
	"github.com/nechja/hellarch/golibs/status"
)

// Handler returns an http.HandlerFunc that responds with a simple health status.
// Use this when you want to add a health endpoint to an existing mux.
func Handler() http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		httputil.WriteOK(w, map[string]string{"status": status.Healthy})
	}
}

// RegisterRoutes registers health endpoints on the provided mux.
func RegisterRoutes(mux *http.ServeMux) {
	mux.HandleFunc("GET /health", Handler())
}
