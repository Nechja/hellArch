package handler

import (
	"context"
	"encoding/json"
	"log/slog"
	"time"

	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/service"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type OAuthHandler struct {
	oauthService   *oauth.Service
	oauthState     *store.Redis
	userService    *service.User
	sessionService *service.Session
	tokenHandler   *TokenHandler
	security       *SecurityHandler
}

func NewOAuthHandler(
	oauthService *oauth.Service,
	oauthState *store.Redis,
	userService *service.User,
	sessionService *service.Session,
	tokenHandler *TokenHandler,
	security *SecurityHandler,
) *OAuthHandler {
	return &OAuthHandler{
		oauthService:   oauthService,
		oauthState:     oauthState,
		userService:    userService,
		sessionService: sessionService,
		tokenHandler:   tokenHandler,
		security:       security,
	}
}

func (h *OAuthHandler) GetOAuthURL(ctx context.Context, provider oauth.Provider, redirectURI string) (*OAuthURLResult, error) {
	if err := h.security.ValidateRedirectURI(redirectURI); err != nil {
		return nil, status.Error(codes.InvalidArgument, err.Error())
	}

	state, err := GenerateRandomState()
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate state")
	}

	csrfToken, err := GenerateRandomState()
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate csrf token")
	}

	pkceVerifier, err := oauth.GeneratePKCEVerifier()
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate pkce verifier")
	}

	csrfHash := h.security.HashCSRFToken(csrfToken)

	stateData, _ := json.Marshal(map[string]string{
		"redirect_uri":  redirectURI,
		"csrf_hash":     csrfHash,
		"pkce_verifier": pkceVerifier,
		"created_at":    time.Now().Format(time.RFC3339),
	})
	if err := h.oauthState.SetOAuthState(ctx, state, string(stateData), h.security.OAuthStateTTL()); err != nil {
		return nil, status.Error(codes.Internal, "failed to store state")
	}

	authURL, err := h.oauthService.GetAuthURL(provider, state, pkceVerifier)
	if err != nil {
		return nil, status.Error(codes.InvalidArgument, err.Error())
	}

	return &OAuthURLResult{
		AuthURL:   authURL,
		State:     state,
		CSRFToken: csrfToken,
	}, nil
}

func (h *OAuthHandler) HandleCallback(ctx context.Context, provider oauth.Provider, code, state, csrfToken, ipAddress, userAgent, existingUserID string) (*AuthResult, error) {
	stateData, err := h.oauthState.GetOAuthState(ctx, state)
	if err != nil {
		return nil, status.Error(codes.InvalidArgument, "invalid or expired state")
	}
	_ = h.oauthState.DeleteOAuthState(ctx, state)

	var stateInfo map[string]string
	if err := json.Unmarshal([]byte(stateData), &stateInfo); err != nil {
		return nil, status.Error(codes.Internal, "failed to parse state")
	}

	if err := h.security.ValidateCSRFToken(csrfToken, stateInfo["csrf_hash"]); err != nil {
		slog.Warn("CSRF validation failed", "error", err)
		return nil, status.Error(codes.InvalidArgument, "CSRF validation failed")
	}

	pkceVerifier := stateInfo["pkce_verifier"]

	token, err := h.oauthService.Exchange(ctx, provider, code, pkceVerifier)
	if err != nil {
		slog.Error("oauth exchange failed", "error", err, "provider", provider)
		return nil, status.Error(codes.Unauthenticated, "failed to exchange code")
	}

	userInfo, err := h.oauthService.GetUserInfo(ctx, provider, token)
	if err != nil {
		slog.Error("failed to get user info", "error", err, "provider", provider)
		return nil, status.Error(codes.Internal, "failed to get user info")
	}

	var result *service.FindOrCreateResult

	if existingUserID != "" {
		linkResult, err := h.userService.LinkProviderToUser(ctx, existingUserID, userInfo)
		if err != nil {
			slog.Error("failed to link provider to existing user", "error", err, "user_id", existingUserID)
			return nil, err
		}
		result = linkResult
	} else {
		findResult, err := h.userService.FindOrCreateFromOAuth(ctx, userInfo)
		if err != nil {
			slog.Error("failed to find or create user", "error", err)
			return nil, err
		}
		result = findResult

		if result.IsNewUser {
			if err := h.userService.PublishCreated(ctx, result.User, userInfo); err != nil {
				slog.Error("failed to publish user.created event", "error", err, "user_id", result.User.ID)
			}
		}
	}

	accessToken, refreshToken, err := h.tokenHandler.GenerateTokenPair(result.User, userInfo)
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate tokens")
	}

	var sessionToken string
	wasLinked := existingUserID != ""

	if !wasLinked {
		sessionToken, err = h.sessionService.Create(ctx, result.User.ID, ipAddress, userAgent)
		if err != nil {
			return nil, err
		}
	}

	return &AuthResult{
		AccessToken:  accessToken,
		RefreshToken: refreshToken,
		SessionToken: sessionToken,
		ExpiresIn:    int64(AccessTokenDuration.Seconds()),
		User: &UserInfo{
			ID:        result.User.ID,
			Email:     result.User.Email,
			Username:  userInfo.Username,
			AvatarURL: userInfo.AvatarURL,
			Provider:  string(provider),
		},
		RedirectURI: stateInfo["redirect_uri"],
		WasLinked:   wasLinked,
	}, nil
}

func (h *OAuthHandler) IsProviderConfigured(provider oauth.Provider) bool {
	return h.oauthService.IsProviderConfigured(provider)
}

func (h *OAuthHandler) GetConfiguredProviders() []string {
	providers := h.oauthService.GetConfiguredProviders()
	result := make([]string, len(providers))
	for i, p := range providers {
		result[i] = string(p)
	}
	return result
}
