package handler

import (
	"encoding/json"
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/status"
	"github.com/nechja/hellarch/services/events/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Router struct {
	audit      *AuditHandler
	webhook    *WebhookHandler
	delivery   *DeliveryHandler
	deadLetter *DeadLetterHandler
	publish    *PublishHandler
	worker     *WorkerHandler
}

func NewRouter(
	store *store.Store,
	publisher *rabbitmq.Publisher,
	mgmt *rabbitmq.ManagementClient,
) *Router {
	return &Router{
		audit:      NewAuditHandler(store),
		webhook:    NewWebhookHandler(store),
		delivery:   NewDeliveryHandler(store),
		deadLetter: NewDeadLetterHandler(store),
		publish:    NewPublishHandler(publisher),
		worker:     NewWorkerHandler(mgmt),
	}
}

func (r *Router) RegisterRoutes(mux *http.ServeMux) {
	// Audit logs
	mux.Handle("GET /audit-logs", otelhttp.NewHandler(http.HandlerFunc(r.audit.listAuditLogs), "list_audit_logs"))
	mux.Handle("POST /audit-logs", otelhttp.NewHandler(http.HandlerFunc(r.audit.createAuditLog), "create_audit_log"))

	// Webhooks
	mux.Handle("GET /webhooks", otelhttp.NewHandler(http.HandlerFunc(r.webhook.listWebhooks), "list_webhooks"))
	mux.Handle("POST /webhooks", otelhttp.NewHandler(http.HandlerFunc(r.webhook.createWebhook), "create_webhook"))
	mux.Handle("GET /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(r.webhook.getWebhook), "get_webhook"))
	mux.Handle("PUT /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(r.webhook.updateWebhook), "update_webhook"))
	mux.Handle("DELETE /webhooks/{id}", otelhttp.NewHandler(http.HandlerFunc(r.webhook.deleteWebhook), "delete_webhook"))
	mux.Handle("GET /webhooks/by-event/{type...}", otelhttp.NewHandler(http.HandlerFunc(r.webhook.getWebhooksByEventType), "get_webhooks_by_event"))

	// Webhook deliveries
	mux.Handle("GET /webhook-deliveries", otelhttp.NewHandler(http.HandlerFunc(r.delivery.listDeliveries), "list_deliveries"))
	mux.Handle("POST /webhook-deliveries", otelhttp.NewHandler(http.HandlerFunc(r.delivery.createDelivery), "create_delivery"))

	// Event publishing
	mux.Handle("POST /publish", otelhttp.NewHandler(http.HandlerFunc(r.publish.publishEvent), "publish_event"))

	// Dead letters
	mux.Handle("GET /dead-letters", otelhttp.NewHandler(http.HandlerFunc(r.deadLetter.listDeadLetters), "list_dead_letters"))
	mux.Handle("POST /dead-letters", otelhttp.NewHandler(http.HandlerFunc(r.deadLetter.createDeadLetter), "create_dead_letter"))
	mux.Handle("DELETE /dead-letters/{id}", otelhttp.NewHandler(http.HandlerFunc(r.deadLetter.deleteDeadLetter), "delete_dead_letter"))

	// Workers status
	mux.Handle("GET /workers/status", otelhttp.NewHandler(http.HandlerFunc(r.worker.workersStatus), "workers_status"))

	// Health
	mux.HandleFunc("GET /health", health)
}

func writeJSON(w http.ResponseWriter, statusCode int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(statusCode)
	json.NewEncoder(w).Encode(data)
}

func writeError(w http.ResponseWriter, statusCode int, message string) {
	writeJSON(w, statusCode, map[string]string{"error": message})
}

func health(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": status.Healthy})
}
