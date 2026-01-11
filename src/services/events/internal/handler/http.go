package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/health"
	"github.com/nechja/hellarch/golibs/httputil"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/services/events/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Handlers struct {
	audit      *AuditHandler
	webhook    *WebhookHandler
	delivery   *DeliveryHandler
	deadLetter *DeadLetterHandler
	publish    *PublishHandler
	worker     *WorkerHandler
}

func NewHandlers(
	store *store.Store,
	publisher *rabbitmq.Publisher,
	mgmt *rabbitmq.ManagementClient,
) *Handlers {
	return &Handlers{
		audit:      NewAuditHandler(store),
		webhook:    NewWebhookHandler(store),
		delivery:   NewDeliveryHandler(store),
		deadLetter: NewDeadLetterHandler(store),
		publish:    NewPublishHandler(publisher),
		worker:     NewWorkerHandler(mgmt),
	}
}

func (r *Handlers) RegisterRoutes(mux *http.ServeMux) {
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
	health.RegisterRoutes(mux)
}

// Convenience wrappers for httputil functions
func writeJSON(w http.ResponseWriter, statusCode int, data any) {
	httputil.WriteJSON(w, statusCode, data)
}

func writeError(w http.ResponseWriter, statusCode int, message string) {
	httputil.WriteError(w, statusCode, message)
}
