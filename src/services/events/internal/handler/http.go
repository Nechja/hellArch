package handler

import (
	"encoding/json"
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/status"
	"github.com/nechja/hellarch/services/events/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Handler struct {
	store     *store.Store
	publisher *rabbitmq.Publisher
	mgmt      *rabbitmq.ManagementClient
}

func New(store *store.Store, publisher *rabbitmq.Publisher, mgmt *rabbitmq.ManagementClient) *Handler {
	return &Handler{
		store:     store,
		publisher: publisher,
		mgmt:      mgmt,
	}
}

func (h *Handler) RegisterRoutes(mux *http.ServeMux) {
	// Audit logs
	mux.Handle("GET /audit-logs", otelhttp.NewHandler(http.HandlerFunc(h.listAuditLogs), "list_audit_logs"))
	mux.Handle("POST /audit-logs", otelhttp.NewHandler(http.HandlerFunc(h.createAuditLog), "create_audit_log"))

	// Webhooks
	mux.Handle("GET /webhooks", otelhttp.NewHandler(http.HandlerFunc(h.listWebhooks), "list_webhooks"))
	mux.Handle("POST /webhooks", otelhttp.NewHandler(http.HandlerFunc(h.createWebhook), "create_webhook"))
	mux.Handle("GET /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(h.getWebhook), "get_webhook"))
	mux.Handle("PUT /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(h.updateWebhook), "update_webhook"))
	mux.Handle("DELETE /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(h.deleteWebhook), "delete_webhook"))
	mux.Handle("GET /webhooks/by-event/{type...}", otelhttp.NewHandler(http.HandlerFunc(h.getWebhooksByEventType), "get_webhooks_by_event"))

	// Webhook deliveries
	mux.Handle("GET /webhook-deliveries", otelhttp.NewHandler(http.HandlerFunc(h.listDeliveries), "list_deliveries"))
	mux.Handle("POST /webhook-deliveries", otelhttp.NewHandler(http.HandlerFunc(h.createDelivery), "create_delivery"))

	// Event publishing
	mux.Handle("POST /publish", otelhttp.NewHandler(http.HandlerFunc(h.publishEvent), "publish_event"))

	// Dead letters
	mux.Handle("GET /dead-letters", otelhttp.NewHandler(http.HandlerFunc(h.listDeadLetters), "list_dead_letters"))
	mux.Handle("POST /dead-letters", otelhttp.NewHandler(http.HandlerFunc(h.createDeadLetter), "create_dead_letter"))
	mux.Handle("DELETE /dead-letters/{id}", otelhttp.NewHandler(http.HandlerFunc(h.deleteDeadLetter), "delete_dead_letter"))

	// Workers status
	mux.Handle("GET /workers/status", otelhttp.NewHandler(http.HandlerFunc(h.workersStatus), "workers_status"))

	// Health
	mux.HandleFunc("GET /health", h.health)
}

func writeJSON(w http.ResponseWriter, status int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(data)
}

func writeError(w http.ResponseWriter, status int, message string) {
	writeJSON(w, status, map[string]string{"error": message})
}

func (h *Handler) health(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": status.Healthy})
}
