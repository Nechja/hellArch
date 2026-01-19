package handler

import (
	"context"
	"log/slog"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/service"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc"
)

// AuthHandler is a facade that coordinates authentication operations
// by delegating to specialized handlers for OAuth, sessions, tokens, and security.
type AuthHandler struct {
	oauth   *OAuthHandler
	session *SessionHandler
	token   *TokenHandler
	user    *service.User
}

func NewAuthHandler(
	sessionService *service.Session,
	userService *service.User,
	jwtService *jwt.Service,
	oauthService *oauth.Service,
	redis *store.Redis,
	cfg *config.Config,
) *AuthHandler {
	security := NewSecurityHandler(cfg)
	tokenHandler := NewTokenHandler(jwtService, userService, redis)
	sessionHandler := NewSessionHandler(sessionService)
	oauthHandler := NewOAuthHandler(
		oauthService,
		redis,
		userService,
		sessionService,
		tokenHandler,
		security,
	)

	return &AuthHandler{
		oauth:   oauthHandler,
		session: sessionHandler,
		token:   tokenHandler,
		user:    userService,
	}
}

func RegisterAuthServiceServer(s *grpc.Server, h *AuthHandler) {
	slog.Info("auth handler registered (proto registration pending)")
}

// OAuth methods - delegate to OAuthHandler

func (h *AuthHandler) GetOAuthURL(ctx context.Context, provider oauth.Provider, redirectURI string) (*OAuthURLResult, error) {
	return h.oauth.GetOAuthURL(ctx, provider, redirectURI)
}

func (h *AuthHandler) HandleOAuthCallback(ctx context.Context, provider oauth.Provider, code, state, csrfToken, ipAddress, userAgent, existingUserID string) (*AuthResult, error) {
	return h.oauth.HandleCallback(ctx, provider, code, state, csrfToken, ipAddress, userAgent, existingUserID)
}

func (h *AuthHandler) IsProviderConfigured(provider oauth.Provider) bool {
	return h.oauth.IsProviderConfigured(provider)
}

func (h *AuthHandler) GetConfiguredProviders() []string {
	return h.oauth.GetConfiguredProviders()
}

// Session methods - delegate to SessionHandler

func (h *AuthHandler) ValidateSession(ctx context.Context, sessionToken string) (string, error) {
	return h.session.Validate(ctx, sessionToken)
}

// GetSession returns full session data for binding validation
func (h *AuthHandler) GetSession(ctx context.Context, sessionToken string) (*store.RedisSession, error) {
	return h.session.GetSession(ctx, sessionToken)
}

func (h *AuthHandler) InvalidateSession(ctx context.Context, sessionToken string) error {
	return h.session.Invalidate(ctx, sessionToken)
}

// Token methods - delegate to TokenHandler

func (h *AuthHandler) RefreshToken(ctx context.Context, refreshToken string) (string, string, error) {
	return h.token.Refresh(ctx, refreshToken)
}

func (h *AuthHandler) ValidateToken(ctx context.Context, token string) (*jwt.Claims, error) {
	return h.token.Validate(ctx, token)
}

func (h *AuthHandler) GenerateServiceToken(serviceID string) (string, error) {
	return h.token.GenerateServiceToken(serviceID)
}

// User methods - direct delegation to user service

func (h *AuthHandler) GetUserByID(ctx context.Context, userID string) (*store.User, error) {
	return h.user.FindByID(ctx, userID)
}

func (h *AuthHandler) GetLinkedAccounts(ctx context.Context, userID string) ([]*store.LinkedAccount, error) {
	return h.user.GetLinkedAccounts(ctx, userID)
}
