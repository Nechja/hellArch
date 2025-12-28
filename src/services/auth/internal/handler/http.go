package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strings"

	"github.com/nechja/hellarch/auth/internal/oauth"
)

// HTTPHandler handles HTTP endpoints for OAuth flows
type HTTPHandler struct {
	auth *AuthHandler
}

func NewHTTPHandler(auth *AuthHandler) *HTTPHandler {
	return &HTTPHandler{auth: auth}
}

func (h *HTTPHandler) RegisterRoutes(mux *http.ServeMux) {
	mux.HandleFunc("GET /auth/{provider}/login", h.handleOAuthLogin)
	mux.HandleFunc("GET /auth/{provider}/callback", h.handleOAuthCallback)
	mux.HandleFunc("POST /auth/refresh", h.handleRefresh)
	mux.HandleFunc("POST /auth/validate", h.handleValidate)
	mux.HandleFunc("GET /auth/health", h.handleHealth)
}

func (h *HTTPHandler) handleOAuthLogin(w http.ResponseWriter, r *http.Request) {
	providerStr := r.PathValue("provider")
	provider := oauth.Provider(providerStr)

	redirectURI := r.URL.Query().Get("redirect_uri")
	if redirectURI == "" {
		redirectURI = r.URL.Query().Get("redirect")
	}

	authURL, _, err := h.auth.GetOAuthURL(r.Context(), provider, redirectURI)
	if err != nil {
		slog.Error("failed to get oauth url", "error", err, "provider", provider)
		http.Error(w, "Failed to initiate OAuth", http.StatusInternalServerError)
		return
	}

	http.Redirect(w, r, authURL, http.StatusTemporaryRedirect)
}

func (h *HTTPHandler) handleOAuthCallback(w http.ResponseWriter, r *http.Request) {
	providerStr := r.PathValue("provider")
	provider := oauth.Provider(providerStr)

	code := r.URL.Query().Get("code")
	state := r.URL.Query().Get("state")

	if code == "" || state == "" {
		// Log the actual error for debugging but don't echo to user (XSS prevention)
		errorDesc := r.URL.Query().Get("error_description")
		if errorDesc == "" {
			errorDesc = r.URL.Query().Get("error")
		}
		slog.Error("oauth callback error", "error", errorDesc, "provider", provider)
		http.Error(w, "OAuth authentication failed. Please try again.", http.StatusBadRequest)
		return
	}

	// Extract client info for session audit trail
	ipAddress := getClientIP(r)
	userAgent := r.UserAgent()

	result, err := h.auth.HandleOAuthCallback(r.Context(), provider, code, state, ipAddress, userAgent)
	if err != nil {
		slog.Error("oauth callback failed", "error", err, "provider", provider)
		http.Error(w, "Authentication failed", http.StatusUnauthorized)
		return
	}

	http.SetCookie(w, &http.Cookie{
		Name:     "hellarch_session",
		Value:    result.SessionToken,
		Path:     "/",
		HttpOnly: true,
		Secure:   r.TLS != nil,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   30 * 24 * 60 * 60, // 30 days
	})

	if result.RedirectURI != "" {
		http.Redirect(w, r, result.RedirectURI, http.StatusTemporaryRedirect)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]interface{}{
		"access_token":  result.AccessToken,
		"refresh_token": result.RefreshToken,
		"expires_in":    result.ExpiresIn,
		"user": map[string]string{
			"id":         result.User.ID,
			"email":      result.User.Email,
			"username":   result.User.Username,
			"avatar_url": result.User.AvatarURL,
			"provider":   result.User.Provider,
		},
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *HTTPHandler) handleRefresh(w http.ResponseWriter, r *http.Request) {
	var req struct {
		RefreshToken string `json:"refresh_token"`
	}

	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	accessToken, refreshToken, err := h.auth.RefreshToken(r.Context(), req.RefreshToken)
	if err != nil {
		http.Error(w, "Invalid refresh token", http.StatusUnauthorized)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]interface{}{
		"access_token":  accessToken,
		"refresh_token": refreshToken,
		"expires_in":    int64(AccessTokenDuration.Seconds()),
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *HTTPHandler) handleValidate(w http.ResponseWriter, r *http.Request) {
	sessionToken := r.Header.Get("X-Session-Token")
	if sessionToken != "" {
		userID, err := h.auth.ValidateSession(r.Context(), sessionToken)
		if err == nil {
			w.Header().Set("Content-Type", "application/json")
			if err := json.NewEncoder(w).Encode(map[string]interface{}{
				"valid":   true,
				"user_id": userID,
			}); err != nil {
				slog.Error("failed to encode response", "error", err)
			}
			return
		}
	}

	authHeader := r.Header.Get("Authorization")
	if authHeader != "" && strings.HasPrefix(authHeader, "Bearer ") {
		token := strings.TrimPrefix(authHeader, "Bearer ")
		claims, err := h.auth.ValidateToken(r.Context(), token)
		if err == nil {
			w.Header().Set("Content-Type", "application/json")
			if err := json.NewEncoder(w).Encode(map[string]interface{}{
				"valid":    true,
				"user_id":  claims.UserID,
				"email":    claims.Email,
				"username": claims.Username,
			}); err != nil {
				slog.Error("failed to encode response", "error", err)
			}
			return
		}
	}

	w.WriteHeader(http.StatusUnauthorized)
	if err := json.NewEncoder(w).Encode(map[string]interface{}{
		"valid": false,
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *HTTPHandler) handleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]string{
		"status": "healthy",
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func getClientIP(r *http.Request) string {
	if xff := r.Header.Get("X-Forwarded-For"); xff != "" {
		if idx := strings.Index(xff, ","); idx != -1 {
			return strings.TrimSpace(xff[:idx])
		}
		return strings.TrimSpace(xff)
	}

	if xri := r.Header.Get("X-Real-IP"); xri != "" {
		return strings.TrimSpace(xri)
	}

	addr := r.RemoteAddr
	if idx := strings.LastIndex(addr, ":"); idx != -1 {
		return addr[:idx]
	}
	return addr
}
