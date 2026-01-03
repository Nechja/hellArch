package handler

import (
	"encoding/json"
	"net/http"

	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/golibs/status"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Handler struct {
	store         *store.Store
	storage       *storage.MinioStorage
	publisher     *rabbitmq.Publisher
	maxUploadSize int64
}

func New(store *store.Store, storage *storage.MinioStorage, publisher *rabbitmq.Publisher, maxUploadSize int64) *Handler {
	return &Handler{
		store:         store,
		storage:       storage,
		publisher:     publisher,
		maxUploadSize: maxUploadSize,
	}
}

func (h *Handler) RegisterRoutes(mux *http.ServeMux) {
	// Upload
	mux.Handle("POST /api/files/upload", otelhttp.NewHandler(http.HandlerFunc(h.uploadFile), "upload_file"))
	mux.Handle("POST /api/files/presign/upload", otelhttp.NewHandler(http.HandlerFunc(h.presignUpload), "presign_upload"))
	mux.Handle("POST /api/files/{id}/confirm", otelhttp.NewHandler(http.HandlerFunc(h.confirmUpload), "confirm_upload"))

	// Download
	mux.Handle("GET /api/files/{id}", otelhttp.NewHandler(http.HandlerFunc(h.downloadFile), "download_file"))
	mux.Handle("GET /api/files/{id}/presign", otelhttp.NewHandler(http.HandlerFunc(h.presignDownload), "presign_download"))

	// Metadata
	mux.Handle("GET /api/files/{id}/meta", otelhttp.NewHandler(http.HandlerFunc(h.getFileMeta), "get_file_meta"))
	mux.Handle("GET /api/files", otelhttp.NewHandler(http.HandlerFunc(h.listFiles), "list_files"))
	mux.Handle("DELETE /api/files/{id}", otelhttp.NewHandler(http.HandlerFunc(h.deleteFile), "delete_file"))

	// Variants
	mux.Handle("GET /api/files/{id}/variants", otelhttp.NewHandler(http.HandlerFunc(h.listVariants), "list_variants"))
	mux.Handle("GET /api/files/{id}/variants/{type}", otelhttp.NewHandler(http.HandlerFunc(h.downloadVariant), "download_variant"))
	mux.Handle("POST /api/files/{id}/variants", otelhttp.NewHandler(http.HandlerFunc(h.createVariant), "create_variant"))

	// Health
	mux.HandleFunc("GET /health", h.health)
}

func writeJSON(w http.ResponseWriter, statusCode int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(statusCode)
	json.NewEncoder(w).Encode(data)
}

func writeError(w http.ResponseWriter, statusCode int, message string) {
	writeJSON(w, statusCode, map[string]string{"error": message})
}

func (h *Handler) health(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": status.Healthy})
}
