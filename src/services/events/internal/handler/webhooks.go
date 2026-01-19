package handler

import (
	"encoding/json"
	"errors"
	"log/slog"
	"net"
	"net/http"
	"net/url"
	"regexp"
	"strconv"

	"github.com/nechja/hellarch/services/events/internal/store"
)

var (
	// ErrInvalidWebhookURL is returned when a webhook URL fails security validation
	ErrInvalidWebhookURL = errors.New("invalid webhook URL")
	// ErrInvalidEventType is returned when an event type has invalid format
	ErrInvalidEventType = errors.New("invalid event type format")
)

// eventTypePattern validates event types like "user.created", "file.uploaded"
var eventTypePattern = regexp.MustCompile(`^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$`)

// validateWebhookURL performs SSRF-prevention validation on webhook URLs
func validateWebhookURL(rawURL string) error {
	parsed, err := url.Parse(rawURL)
	if err != nil {
		return ErrInvalidWebhookURL
	}

	// Must be HTTP or HTTPS (HTTPS preferred in production)
	if parsed.Scheme != "http" && parsed.Scheme != "https" {
		return ErrInvalidWebhookURL
	}

	// Extract hostname
	hostname := parsed.Hostname()
	if hostname == "" {
		return ErrInvalidWebhookURL
	}

	// Block localhost and loopback
	if hostname == "localhost" || hostname == "127.0.0.1" || hostname == "::1" {
		slog.Warn("blocked webhook to localhost", "url", rawURL)
		return ErrInvalidWebhookURL
	}

	// Resolve hostname to IP and check for private/internal ranges
	// SECURITY: Reject unresolvable hostnames to prevent SSRF via DNS rebinding
	// where a hostname might resolve to a private IP at delivery time
	ips, err := net.LookupIP(hostname)
	if err != nil {
		slog.Warn("rejected webhook with unresolvable hostname", "hostname", hostname, "error", err)
		return ErrInvalidWebhookURL
	}

	for _, ip := range ips {
		if isPrivateIP(ip) {
			slog.Warn("blocked webhook to private IP", "url", rawURL, "ip", ip.String())
			return ErrInvalidWebhookURL
		}
	}

	return nil
}

// isPrivateIP checks if an IP address is in a private/internal range
func isPrivateIP(ip net.IP) bool {
	// Check for loopback
	if ip.IsLoopback() {
		return true
	}

	// Check for link-local (169.254.x.x - includes cloud metadata endpoints)
	if ip.IsLinkLocalUnicast() || ip.IsLinkLocalMulticast() {
		return true
	}

	// Check for private ranges (RFC 1918)
	privateRanges := []string{
		"10.0.0.0/8",
		"172.16.0.0/12",
		"192.168.0.0/16",
		"fc00::/7", // IPv6 unique local
	}

	for _, cidr := range privateRanges {
		_, network, err := net.ParseCIDR(cidr)
		if err != nil {
			continue
		}
		if network.Contains(ip) {
			return true
		}
	}

	// Cloud metadata endpoint (AWS, GCP, Azure)
	if ip.Equal(net.ParseIP("169.254.169.254")) {
		return true
	}

	return false
}

// validateEventTypes checks that all event types match the expected format
func validateEventTypes(eventTypes []string) error {
	for _, et := range eventTypes {
		if !eventTypePattern.MatchString(et) {
			return ErrInvalidEventType
		}
	}
	return nil
}

// webhookResponse is a sanitized webhook response that masks the secret
type webhookResponse struct {
	ID         string              `json:"id"`
	Name       string              `json:"name"`
	URL        string              `json:"url"`
	HasSecret  bool                `json:"has_secret"`
	EventTypes []string            `json:"event_types"`
	Format     store.WebhookFormat `json:"format"`
	IsActive   bool                `json:"is_active"`
	CreatedAt  string              `json:"created_at"`
	UpdatedAt  string              `json:"updated_at"`
}

// sanitizeWebhook converts a Webhook to a response that hides the secret
func sanitizeWebhook(w *store.Webhook) *webhookResponse {
	return &webhookResponse{
		ID:         w.ID,
		Name:       w.Name,
		URL:        w.URL,
		HasSecret:  w.Secret != "",
		EventTypes: w.EventTypes,
		Format:     w.Format,
		IsActive:   w.IsActive,
		CreatedAt:  w.CreatedAt.Format("2006-01-02T15:04:05Z07:00"),
		UpdatedAt:  w.UpdatedAt.Format("2006-01-02T15:04:05Z07:00"),
	}
}

// sanitizeWebhooks converts a list of webhooks to sanitized responses
func sanitizeWebhooks(webhooks []*store.Webhook) []*webhookResponse {
	result := make([]*webhookResponse, len(webhooks))
	for i, w := range webhooks {
		result[i] = sanitizeWebhook(w)
	}
	return result
}

type WebhookHandler struct {
	store *store.Store
}

func NewWebhookHandler(store *store.Store) *WebhookHandler {
	return &WebhookHandler{store: store}
}

type createWebhookRequest struct {
	Name       string `json:"name"`
	URL        string `json:"url"`
	Secret     string `json:"secret"`
	EventTypes []string `json:"event_types"`
	Format     string `json:"format"` // raw, discord, slack
}

type updateWebhookRequest struct {
	Name       *string  `json:"name,omitempty"`
	URL        *string  `json:"url,omitempty"`
	Secret     *string  `json:"secret,omitempty"`
	EventTypes []string `json:"event_types,omitempty"`
	Format     *string  `json:"format,omitempty"`
	IsActive   *bool    `json:"is_active,omitempty"`
}

func (h *WebhookHandler) listWebhooks(w http.ResponseWriter, r *http.Request) {
	limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
	offset, _ := strconv.Atoi(r.URL.Query().Get("offset"))

	webhooks, total, err := h.store.ListWebhooks(r.Context(), limit, offset)
	if err != nil {
		slog.Error("failed to list webhooks", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to list webhooks")
		return
	}

	// Return sanitized webhooks without secrets
	writeJSON(w, http.StatusOK, map[string]any{
		"data":  sanitizeWebhooks(webhooks),
		"total": total,
	})
}

func (h *WebhookHandler) createWebhook(w http.ResponseWriter, r *http.Request) {
	var req createWebhookRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.Name == "" || req.URL == "" || req.Secret == "" {
		writeError(w, http.StatusBadRequest, "name, url, and secret are required")
		return
	}

	// Validate URL to prevent SSRF attacks
	if err := validateWebhookURL(req.URL); err != nil {
		writeError(w, http.StatusBadRequest, "invalid webhook URL: must be a public HTTP(S) endpoint")
		return
	}

	// Validate event types format
	if len(req.EventTypes) > 0 {
		if err := validateEventTypes(req.EventTypes); err != nil {
			writeError(w, http.StatusBadRequest, "invalid event type format: must be lowercase dot-separated (e.g., user.created)")
			return
		}
	}

	// Validate format
	format := store.WebhookFormat(req.Format)
	if format == "" {
		format = store.WebhookFormatRaw
	}
	if format != store.WebhookFormatRaw && format != store.WebhookFormatDiscord && format != store.WebhookFormatSlack {
		writeError(w, http.StatusBadRequest, "format must be one of: raw, discord, slack")
		return
	}

	webhook, err := h.store.CreateWebhook(r.Context(), store.CreateWebhookParams{
		Name:       req.Name,
		URL:        req.URL,
		Secret:     req.Secret,
		EventTypes: req.EventTypes,
		Format:     format,
	})
	if err != nil {
		slog.Error("failed to create webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to create webhook")
		return
	}

	// Return sanitized webhook without secret
	writeJSON(w, http.StatusCreated, sanitizeWebhook(webhook))
}

func (h *WebhookHandler) getWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	webhook, err := h.store.GetWebhook(r.Context(), id)
	if err != nil {
		slog.Error("failed to get webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to get webhook")
		return
	}
	if webhook == nil {
		writeError(w, http.StatusNotFound, "Webhook not found")
		return
	}

	// Return sanitized webhook without secret
	writeJSON(w, http.StatusOK, sanitizeWebhook(webhook))
}

func (h *WebhookHandler) updateWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	var req updateWebhookRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	// Validate URL if provided to prevent SSRF attacks
	if req.URL != nil {
		if err := validateWebhookURL(*req.URL); err != nil {
			writeError(w, http.StatusBadRequest, "invalid webhook URL: must be a public HTTP(S) endpoint")
			return
		}
	}

	// Validate event types format if provided
	if len(req.EventTypes) > 0 {
		if err := validateEventTypes(req.EventTypes); err != nil {
			writeError(w, http.StatusBadRequest, "invalid event type format: must be lowercase dot-separated (e.g., user.created)")
			return
		}
	}

	// Validate format if provided
	var format *store.WebhookFormat
	if req.Format != nil {
		f := store.WebhookFormat(*req.Format)
		if f != store.WebhookFormatRaw && f != store.WebhookFormatDiscord && f != store.WebhookFormatSlack {
			writeError(w, http.StatusBadRequest, "format must be one of: raw, discord, slack")
			return
		}
		format = &f
	}

	webhook, err := h.store.UpdateWebhook(r.Context(), id, store.UpdateWebhookParams{
		Name:       req.Name,
		URL:        req.URL,
		Secret:     req.Secret,
		EventTypes: req.EventTypes,
		Format:     format,
		IsActive:   req.IsActive,
	})
	if err != nil {
		slog.Error("failed to update webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to update webhook")
		return
	}
	if webhook == nil {
		writeError(w, http.StatusNotFound, "Webhook not found")
		return
	}

	// Return sanitized webhook without secret
	writeJSON(w, http.StatusOK, sanitizeWebhook(webhook))
}

func (h *WebhookHandler) deleteWebhook(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	if err := h.store.DeleteWebhook(r.Context(), id); err != nil {
		slog.Error("failed to delete webhook", "error", err)
		writeError(w, http.StatusInternalServerError, "Failed to delete webhook")
		return
	}

	w.WriteHeader(http.StatusNoContent)
}

func (h *WebhookHandler) getWebhooksByEventType(w http.ResponseWriter, r *http.Request) {
	eventType := r.PathValue("type")

	// Validate event type format
	if err := validateEventTypes([]string{eventType}); err != nil {
		writeError(w, http.StatusBadRequest, "invalid event type format: must be lowercase dot-separated (e.g., user.created)")
		return
	}

	webhooks, err := h.store.GetWebhooksForEventType(r.Context(), eventType)
	if err != nil {
		slog.Error("failed to get webhooks for event type", "error", err, "event_type", eventType)
		writeError(w, http.StatusInternalServerError, "Failed to get webhooks")
		return
	}

	// Return sanitized webhooks without secrets
	writeJSON(w, http.StatusOK, map[string]any{
		"data": sanitizeWebhooks(webhooks),
	})
}
