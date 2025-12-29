package handler

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"

	"github.com/nechja/hellarch/auth/internal/store"
)

type AdminHandler struct {
	postgres *store.Postgres
	redis    *store.Redis
}

func NewAdminHandler(postgres *store.Postgres, redis *store.Redis) *AdminHandler {
	return &AdminHandler{
		postgres: postgres,
		redis:    redis,
	}
}

func (h *AdminHandler) RegisterRoutes(mux *http.ServeMux) {
	mux.HandleFunc("GET /auth/admin/users", h.handleListUsers)
	mux.HandleFunc("GET /auth/admin/users/{id}", h.handleGetUser)
	mux.HandleFunc("GET /auth/admin/users/{id}/sessions", h.handleGetUserSessions)
	mux.HandleFunc("DELETE /auth/admin/sessions/{id}", h.handleDeleteSession)
	mux.HandleFunc("GET /auth/admin/stats", h.handleGetStats)

	// Permission management
	mux.HandleFunc("GET /auth/admin/users/{id}/permissions", h.handleGetUserPermissions)
	mux.HandleFunc("PUT /auth/admin/users/{id}/permissions", h.handleSetUserPermissions)
	mux.HandleFunc("POST /auth/admin/users/{id}/permissions/grant", h.handleGrantPermission)
	mux.HandleFunc("POST /auth/admin/users/{id}/permissions/revoke", h.handleRevokePermission)

	// User status
	mux.HandleFunc("POST /auth/admin/users/{id}/activate", h.handleActivateUser)
	mux.HandleFunc("POST /auth/admin/users/{id}/deactivate", h.handleDeactivateUser)
}

func (h *AdminHandler) handleListUsers(w http.ResponseWriter, r *http.Request) {
	page, _ := strconv.Atoi(r.URL.Query().Get("page"))
	if page < 1 {
		page = 1
	}
	pageSize, _ := strconv.Atoi(r.URL.Query().Get("pageSize"))
	if pageSize < 1 || pageSize > 100 {
		pageSize = 20
	}

	offset := (page - 1) * pageSize

	users, total, err := h.postgres.ListUsers(r.Context(), pageSize, offset)
	if err != nil {
		slog.Error("failed to list users", "error", err)
		http.Error(w, "Failed to list users", http.StatusInternalServerError)
		return
	}

	response := map[string]any{
		"users":       users,
		"total_count": total,
		"page":        page,
		"page_size":   pageSize,
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(response); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleGetUser(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	user, err := h.postgres.GetUserByID(r.Context(), userID)
	if err != nil {
		slog.Debug("user not found", "user_id", userID, "error", err)
		http.Error(w, "User not found", http.StatusNotFound)
		return
	}

	linkedAccounts, err := h.postgres.GetLinkedAccounts(r.Context(), userID)
	if err != nil {
		slog.Error("failed to get linked accounts", "user_id", userID, "error", err)
		linkedAccounts = []*store.LinkedAccount{}
	}

	response := map[string]any{
		"id":              user.ID,
		"email":           user.Email,
		"username":        user.Username,
		"is_active":       user.IsActive,
		"permissions":     user.Permissions,
		"created_at":      user.CreatedAt,
		"updated_at":      user.UpdatedAt,
		"linked_accounts": linkedAccounts,
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(response); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleGetUserSessions(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	sessions, err := h.postgres.GetUserSessions(r.Context(), userID)
	if err != nil {
		slog.Error("failed to get user sessions", "user_id", userID, "error", err)
		http.Error(w, "Failed to get sessions", http.StatusInternalServerError)
		return
	}

	sanitizedSessions := make([]map[string]any, len(sessions))
	for i, s := range sessions {
		sanitizedSessions[i] = map[string]any{
			"id":         s.ID,
			"user_id":    s.UserID,
			"ip_address": s.IPAddress,
			"user_agent": s.UserAgent,
			"expires_at": s.ExpiresAt,
			"created_at": s.CreatedAt,
		}
	}

	response := map[string]any{
		"sessions": sanitizedSessions,
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(response); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleDeleteSession(w http.ResponseWriter, r *http.Request) {
	sessionID := r.PathValue("id")

	if err := h.postgres.DeleteSession(r.Context(), sessionID); err != nil {
		slog.Error("failed to delete session", "session_id", sessionID, "error", err)
		http.Error(w, "Failed to delete session", http.StatusInternalServerError)
		return
	}

	_ = h.redis.InvalidateSession(r.Context(), sessionID)

	slog.Info("session revoked", "session_id", sessionID)

	w.WriteHeader(http.StatusNoContent)
}

func (h *AdminHandler) handleGetStats(w http.ResponseWriter, r *http.Request) {
	userCount, err := h.postgres.CountUsers(r.Context())
	if err != nil {
		slog.Error("failed to count users", "error", err)
		userCount = 0
	}

	sessionCount, err := h.postgres.CountActiveSessions(r.Context())
	if err != nil {
		slog.Error("failed to count sessions", "error", err)
		sessionCount = 0
	}

	linkedAccountCount, err := h.postgres.CountLinkedAccounts(r.Context())
	if err != nil {
		slog.Error("failed to count linked accounts", "error", err)
		linkedAccountCount = 0
	}

	response := map[string]any{
		"user_count":           userCount,
		"session_count":        sessionCount,
		"linked_account_count": linkedAccountCount,
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(response); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleGetUserPermissions(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	permissions, err := h.postgres.GetUserPermissions(r.Context(), userID)
	if err != nil {
		slog.Error("failed to get user permissions", "user_id", userID, "error", err)
		http.Error(w, "User not found", http.StatusNotFound)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]any{
		"user_id":     userID,
		"permissions": permissions,
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleSetUserPermissions(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	var req struct {
		Permissions map[string]bool `json:"permissions"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if err := h.postgres.SetUserPermissions(r.Context(), userID, req.Permissions); err != nil {
		slog.Error("failed to set user permissions", "user_id", userID, "error", err)
		http.Error(w, "Failed to update permissions", http.StatusInternalServerError)
		return
	}

	slog.Info("user permissions updated", "user_id", userID, "permissions", req.Permissions)

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(map[string]any{
		"user_id":     userID,
		"permissions": req.Permissions,
	}); err != nil {
		slog.Error("failed to encode response", "error", err)
	}
}

func (h *AdminHandler) handleGrantPermission(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	var req struct {
		Permission string `json:"permission"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if req.Permission == "" {
		http.Error(w, "Permission is required", http.StatusBadRequest)
		return
	}

	if err := h.postgres.GrantPermission(r.Context(), userID, req.Permission); err != nil {
		slog.Error("failed to grant permission", "user_id", userID, "permission", req.Permission, "error", err)
		http.Error(w, "Failed to grant permission", http.StatusInternalServerError)
		return
	}

	slog.Info("permission granted", "user_id", userID, "permission", req.Permission)

	w.WriteHeader(http.StatusNoContent)
}

func (h *AdminHandler) handleRevokePermission(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	var req struct {
		Permission string `json:"permission"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if req.Permission == "" {
		http.Error(w, "Permission is required", http.StatusBadRequest)
		return
	}

	if err := h.postgres.RevokePermission(r.Context(), userID, req.Permission); err != nil {
		slog.Error("failed to revoke permission", "user_id", userID, "permission", req.Permission, "error", err)
		http.Error(w, "Failed to revoke permission", http.StatusInternalServerError)
		return
	}

	slog.Info("permission revoked", "user_id", userID, "permission", req.Permission)

	w.WriteHeader(http.StatusNoContent)
}

func (h *AdminHandler) handleActivateUser(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	if err := h.postgres.SetUserActive(r.Context(), userID, true); err != nil {
		slog.Error("failed to activate user", "user_id", userID, "error", err)
		http.Error(w, "Failed to activate user", http.StatusInternalServerError)
		return
	}

	slog.Info("user activated", "user_id", userID)

	w.WriteHeader(http.StatusNoContent)
}

func (h *AdminHandler) handleDeactivateUser(w http.ResponseWriter, r *http.Request) {
	userID := r.PathValue("id")

	if err := h.postgres.SetUserActive(r.Context(), userID, false); err != nil {
		slog.Error("failed to deactivate user", "user_id", userID, "error", err)
		http.Error(w, "Failed to deactivate user", http.StatusInternalServerError)
		return
	}

	// Invalidate all user sessions when deactivating
	sessions, err := h.postgres.GetUserSessions(r.Context(), userID)
	if err == nil {
		for _, s := range sessions {
			_ = h.redis.InvalidateSession(r.Context(), s.Token)
			_ = h.postgres.DeleteSession(r.Context(), s.ID)
		}
		slog.Info("invalidated user sessions on deactivation", "user_id", userID, "session_count", len(sessions))
	}

	slog.Info("user deactivated", "user_id", userID)

	w.WriteHeader(http.StatusNoContent)
}
