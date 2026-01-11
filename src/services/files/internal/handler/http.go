package handler

import (
	"net/http"

	"github.com/nechja/hellarch/golibs/health"
	"github.com/nechja/hellarch/golibs/httputil"
	"github.com/nechja/hellarch/services/files/internal/service"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Handler struct {
	uploadService  *service.Upload
	fileService    *service.File
	variantService *service.Variant
}

func New(
	uploadService *service.Upload,
	fileService *service.File,
	variantService *service.Variant,
) *Handler {
	return &Handler{
		uploadService:  uploadService,
		fileService:    fileService,
		variantService: variantService,
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
	health.RegisterRoutes(mux)
}

// Convenience wrappers for httputil functions
func writeJSON(w http.ResponseWriter, statusCode int, data any) {
	httputil.WriteJSON(w, statusCode, data)
}

func writeError(w http.ResponseWriter, statusCode int, message string) {
	httputil.WriteError(w, statusCode, message)
}
