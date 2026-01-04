package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strings"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/golibs/status"
	"google.golang.org/grpc/codes"
	grpcstatus "google.golang.org/grpc/status"
)

// HTTPHandler handles HTTP endpoints for OAuth flows
type HTTPHandler struct {
	auth   *AuthHandler
	config *config.Config
}

func NewHTTPHandler(auth *AuthHandler, cfg *config.Config) *HTTPHandler {
	return &HTTPHandler{auth: auth, config: cfg}
}

func (h *HTTPHandler) RegisterRoutes(mux *http.ServeMux) {
	mux.HandleFunc("GET /auth/{provider}/login", h.handleOAuthLogin)
	mux.HandleFunc("GET /auth/{provider}/callback", h.handleOAuthCallback)
	mux.HandleFunc("POST /auth/refresh", h.handleRefresh)
	mux.HandleFunc("POST /auth/validate", h.handleValidate)
	mux.HandleFunc("GET /auth/me", h.handleMe)
	mux.HandleFunc("GET /auth/logout", h.handleLogout)
	mux.HandleFunc("POST /auth/logout", h.handleLogout)
	mux.HandleFunc("GET /auth/health", h.handleHealth)
}

const csrfCookieName = "hellarch_csrf"

func (h *HTTPHandler) handleOAuthLogin(w http.ResponseWriter, r *http.Request) {
	providerStr := r.PathValue("provider")
	provider := oauth.Provider(providerStr)

	redirectURI := r.URL.Query().Get("redirect_uri")
	if redirectURI == "" {
		redirectURI = r.URL.Query().Get("redirect")
	}

	result, err := h.auth.GetOAuthURL(r.Context(), provider, redirectURI)
	if err != nil {
		slog.Error("failed to get oauth url", "error", err, "provider", provider)
		http.Error(w, "Failed to initiate OAuth", http.StatusInternalServerError)
		return
	}

	// Set CSRF cookie for browser binding - ties the OAuth flow to this browser
	http.SetCookie(w, &http.Cookie{
		Name:     csrfCookieName,
		Value:    result.CSRFToken,
		Path:     "/",
		HttpOnly: true,
		Secure:   h.config.SecureCookies,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   600, // 10 minutes - matches OAuthStateTTL
	})

	http.Redirect(w, r, result.AuthURL, http.StatusTemporaryRedirect)
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

	// Read CSRF token from cookie - this proves the callback is from the same browser
	csrfCookie, err := r.Cookie(csrfCookieName)
	if err != nil {
		slog.Warn("missing CSRF cookie in OAuth callback", "provider", provider)
		http.Error(w, "Session expired. Please try logging in again.", http.StatusBadRequest)
		return
	}
	csrfToken := csrfCookie.Value

	// Clear the CSRF cookie
	http.SetCookie(w, &http.Cookie{
		Name:     csrfCookieName,
		Value:    "",
		Path:     "/",
		HttpOnly: true,
		Secure:   h.config.SecureCookies,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   -1,
	})

	// Extract client info for session audit trail
	ipAddress := getClientIP(r)
	userAgent := r.UserAgent()

	// Check if user is already logged in (for account linking)
	var existingUserID string
	if sessionCookie, err := r.Cookie("hellarch_session"); err == nil {
		if userID, err := h.auth.ValidateSession(r.Context(), sessionCookie.Value); err == nil {
			existingUserID = userID
		}
	}

	result, err := h.auth.HandleOAuthCallback(r.Context(), provider, code, state, csrfToken, ipAddress, userAgent, existingUserID)
	if err != nil {
		if grpcstatus.Code(err) == codes.AlreadyExists {
			http.Redirect(w, r, "/login?error=email_conflict", http.StatusTemporaryRedirect)
			return
		}
		slog.Error("oauth callback failed", "error", err, "provider", provider)
		http.Error(w, "Authentication failed", http.StatusUnauthorized)
		return
	}

	http.SetCookie(w, &http.Cookie{
		Name:     "hellarch_session",
		Value:    result.SessionToken,
		Path:     "/",
		HttpOnly: true,
		Secure:   h.config.SecureCookies,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   24 * 60 * 60, // 24 hours
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
			// Fetch full user to get permissions and is_active
			user, err := h.auth.GetUserByID(r.Context(), userID)
			if err != nil {
				slog.Error("failed to get user", "error", err, "user_id", userID)
				// Return 401 instead of 500 to avoid info disclosure about internal failures
				w.WriteHeader(http.StatusUnauthorized)
				return
			}

			// Check if user is active - use 401 since the session is no longer valid for this user
			if !user.IsActive {
				w.WriteHeader(http.StatusUnauthorized)
				if err := json.NewEncoder(w).Encode(map[string]interface{}{
					"valid": false,
					"error": "user_inactive",
				}); err != nil {
					slog.Error("failed to encode response", "error", err)
				}
				return
			}

			w.Header().Set("Content-Type", "application/json")
			if err := json.NewEncoder(w).Encode(map[string]interface{}{
				"valid":       true,
				"user_id":     user.ID,
				"email":       user.Email,
				"username":    user.Username,
				"is_active":   user.IsActive,
				"permissions": user.Permissions,
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
			// For JWT, permissions are in the custom claims
			w.Header().Set("Content-Type", "application/json")
			response := map[string]interface{}{
				"valid":    true,
				"user_id":  claims.UserID,
				"email":    claims.Email,
				"username": claims.Username,
			}
			// Parse permissions from custom claims if present
			if permStr, ok := claims.Custom["permissions"]; ok {
				var perms map[string]bool
				if err := json.Unmarshal([]byte(permStr), &perms); err == nil {
					response["permissions"] = perms
				}
			}
			if err := json.NewEncoder(w).Encode(response); err != nil {
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
		"status": status.Healthy,
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *HTTPHandler) handleLogout(w http.ResponseWriter, r *http.Request) {
	sessionToken := r.Header.Get("X-Session-Token")
	if sessionToken == "" {
		cookie, err := r.Cookie("hellarch_session")
		if err == nil {
			sessionToken = cookie.Value
		}
	}

	if sessionToken != "" {
		if err := h.auth.InvalidateSession(r.Context(), sessionToken); err != nil {
			slog.Warn("failed to invalidate session", "error", err)
		}
	}

	http.SetCookie(w, &http.Cookie{
		Name:     "hellarch_session",
		Value:    "",
		Path:     "/",
		HttpOnly: true,
		Secure:   h.config.SecureCookies,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   -1,
	})

	http.Redirect(w, r, "/login", http.StatusTemporaryRedirect)
}

func (h *HTTPHandler) handleMe(w http.ResponseWriter, r *http.Request) {
	sessionToken := r.Header.Get("X-Session-Token")
	if sessionToken == "" {
		cookie, err := r.Cookie("hellarch_session")
		if err == nil {
			sessionToken = cookie.Value
		}
	}

	if sessionToken == "" {
		w.WriteHeader(http.StatusUnauthorized)
		if err := json.NewEncoder(w).Encode(map[string]string{
			"error": "no session token",
		}); err != nil {
			slog.Error("failed to encode response", "error", err)
		}
		return
	}

	userID, err := h.auth.ValidateSession(r.Context(), sessionToken)
	if err != nil {
		w.WriteHeader(http.StatusUnauthorized)
		if err := json.NewEncoder(w).Encode(map[string]string{
			"error": "invalid session",
		}); err != nil {
			slog.Error("failed to encode response", "error", err)
		}
		return
	}

	// Fetch linked accounts
	accounts, err := h.auth.GetLinkedAccounts(r.Context(), userID)
	if err != nil {
		slog.Error("failed to get linked accounts", "error", err)
		accounts = nil
	}

	// Build provider list
	var providers []string
	for _, acc := range accounts {
		providers = append(providers, acc.Provider)
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]any{
		"user_id":          userID,
		"linked_providers": providers,
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
