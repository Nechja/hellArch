package handler

import (
	"context"
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"encoding/json"
	"errors"
	"log/slog"
	"net/url"
	"strings"
	"time"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/service"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type AuthHandler struct {
	sessionService *service.Session
	userService    *service.User
	jwtService     *jwt.Service
	oauthService   *oauth.Service
	oauthState     *store.Redis // Only for OAuth state management
	config         *config.Config
}

func NewAuthHandler(
	sessionService *service.Session,
	userService *service.User,
	jwtService *jwt.Service,
	oauthService *oauth.Service,
	redis *store.Redis,
	cfg *config.Config,
) *AuthHandler {
	return &AuthHandler{
		sessionService: sessionService,
		userService:    userService,
		jwtService:     jwtService,
		oauthService:   oauthService,
		oauthState:     redis,
		config:         cfg,
	}
}

func RegisterAuthServiceServer(s *grpc.Server, h *AuthHandler) {
	slog.Info("auth handler registered (proto registration pending)")
}

type OAuthURLResult struct {
	AuthURL   string
	State     string
	CSRFToken string
}

func (h *AuthHandler) GetOAuthURL(ctx context.Context, provider oauth.Provider, redirectURI string) (*OAuthURLResult, error) {
	if err := h.validateRedirectURI(redirectURI); err != nil {
		return nil, status.Error(codes.InvalidArgument, err.Error())
	}

	state, err := generateRandomState()
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate state")
	}

	csrfToken, err := generateRandomState()
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate csrf token")
	}

	csrfHash := sha256.Sum256([]byte(csrfToken))

	stateData, _ := json.Marshal(map[string]string{
		"redirect_uri": redirectURI,
		"csrf_hash":    base64.URLEncoding.EncodeToString(csrfHash[:]),
		"created_at":   time.Now().Format(time.RFC3339),
	})
	if err := h.oauthState.SetOAuthState(ctx, state, string(stateData), OAuthStateTTL); err != nil {
		return nil, status.Error(codes.Internal, "failed to store state")
	}

	authURL, err := h.oauthService.GetAuthURL(provider, state)
	if err != nil {
		return nil, status.Error(codes.InvalidArgument, err.Error())
	}

	return &OAuthURLResult{
		AuthURL:   authURL,
		State:     state,
		CSRFToken: csrfToken,
	}, nil
}

func (h *AuthHandler) HandleOAuthCallback(ctx context.Context, provider oauth.Provider, code, state, csrfToken, ipAddress, userAgent string) (*AuthResult, error) {
	stateData, err := h.oauthState.GetOAuthState(ctx, state)
	if err != nil {
		return nil, status.Error(codes.InvalidArgument, "invalid or expired state")
	}
	_ = h.oauthState.DeleteOAuthState(ctx, state)

	var stateInfo map[string]string
	if err := json.Unmarshal([]byte(stateData), &stateInfo); err != nil {
		return nil, status.Error(codes.Internal, "failed to parse state")
	}

	if err := h.validateCSRFToken(csrfToken, stateInfo["csrf_hash"]); err != nil {
		slog.Warn("CSRF validation failed", "error", err)
		return nil, status.Error(codes.InvalidArgument, "CSRF validation failed")
	}

	token, err := h.oauthService.Exchange(ctx, provider, code)
	if err != nil {
		slog.Error("oauth exchange failed", "error", err, "provider", provider)
		return nil, status.Error(codes.Unauthenticated, "failed to exchange code")
	}

	userInfo, err := h.oauthService.GetUserInfo(ctx, provider, token)
	if err != nil {
		slog.Error("failed to get user info", "error", err, "provider", provider)
		return nil, status.Error(codes.Internal, "failed to get user info")
	}

	result, err := h.userService.FindOrCreateFromOAuth(ctx, userInfo)
	if err != nil {
		slog.Error("failed to find or create user", "error", err)
		return nil, err
	}

	if result.IsNewUser {
		if err := h.userService.PublishCreated(ctx, result.User, userInfo); err != nil {
			slog.Error("failed to publish user.created event", "error", err, "user_id", result.User.ID)
		}
	}

	accessToken, refreshToken, err := h.generateTokens(result.User, userInfo)
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate tokens")
	}

	sessionToken, err := h.sessionService.Create(ctx, result.User.ID, ipAddress, userAgent)
	if err != nil {
		return nil, err
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
	}, nil
}

func (h *AuthHandler) ValidateSession(ctx context.Context, sessionToken string) (string, error) {
	session, err := h.sessionService.Validate(ctx, sessionToken)
	if err != nil {
		return "", err
	}
	return session.UserID, nil
}

func (h *AuthHandler) RefreshToken(ctx context.Context, refreshToken string) (string, string, error) {
	claims, err := h.jwtService.ValidateToken(refreshToken)
	if err != nil {
		return "", "", status.Error(codes.Unauthenticated, "invalid refresh token")
	}

	user, err := h.userService.FindByID(ctx, claims.Subject)
	if err != nil {
		return "", "", err
	}

	if !user.IsActive {
		return "", "", status.Error(codes.PermissionDenied, "user is inactive")
	}

	accounts, err := h.userService.GetLinkedAccounts(ctx, user.ID)
	if err != nil || len(accounts) == 0 {
		return "", "", status.Error(codes.Internal, "no linked accounts")
	}

	newClaims := &jwt.Claims{
		UserID:   user.ID,
		Email:    user.Email,
		Username: deref(accounts[0].Username),
		Provider: accounts[0].Provider,
	}

	if user.Permissions != nil && len(user.Permissions) > 0 {
		permJSON, _ := json.Marshal(user.Permissions)
		newClaims.Custom = map[string]string{
			"permissions": string(permJSON),
		}
	}

	accessToken, err := h.jwtService.GenerateAccessToken(newClaims, AccessTokenDuration)
	if err != nil {
		return "", "", status.Error(codes.Internal, "failed to generate access token")
	}

	newRefreshToken, err := h.jwtService.GenerateRefreshToken(user.ID, RefreshTokenDuration)
	if err != nil {
		return "", "", status.Error(codes.Internal, "failed to generate refresh token")
	}

	return accessToken, newRefreshToken, nil
}

func (h *AuthHandler) ValidateToken(ctx context.Context, token string) (*jwt.Claims, error) {
	return h.jwtService.ValidateToken(token)
}

func (h *AuthHandler) GetUserByID(ctx context.Context, userID string) (*store.User, error) {
	return h.userService.FindByID(ctx, userID)
}

func (h *AuthHandler) InvalidateSession(ctx context.Context, sessionToken string) error {
	return h.sessionService.Invalidate(ctx, sessionToken)
}

func (h *AuthHandler) validateRedirectURI(redirectURI string) error {
	if redirectURI == "" {
		return nil
	}

	if strings.HasPrefix(redirectURI, "/") && !strings.HasPrefix(redirectURI, "//") {
		return nil
	}

	parsed, err := url.Parse(redirectURI)
	if err != nil {
		return errors.New("invalid redirect_uri format")
	}

	if !parsed.IsAbs() {
		return errors.New("redirect_uri must be absolute or start with /")
	}

	redirectOrigin := parsed.Scheme + "://" + parsed.Host
	for _, allowed := range h.config.AllowedRedirectURIs {
		if strings.EqualFold(redirectOrigin, allowed) {
			return nil
		}
	}

	slog.Warn("redirect_uri not in allowlist", "uri", redirectURI, "allowed", h.config.AllowedRedirectURIs)
	return errors.New("redirect_uri not allowed")
}

func (h *AuthHandler) validateCSRFToken(csrfToken, storedHash string) error {
	if csrfToken == "" {
		return errors.New("missing csrf token")
	}
	if storedHash == "" {
		return errors.New("missing stored csrf hash")
	}

	computedHash := sha256.Sum256([]byte(csrfToken))
	computedHashB64 := base64.URLEncoding.EncodeToString(computedHash[:])

	if subtle.ConstantTimeCompare([]byte(computedHashB64), []byte(storedHash)) != 1 {
		return errors.New("csrf token mismatch")
	}

	return nil
}

func (h *AuthHandler) generateTokens(user *store.User, info *oauth.UserInfo) (string, string, error) {
	claims := &jwt.Claims{
		UserID:   user.ID,
		Email:    user.Email,
		Username: info.Username,
		Provider: string(info.Provider),
	}

	if user.Permissions != nil && len(user.Permissions) > 0 {
		permJSON, _ := json.Marshal(user.Permissions)
		claims.Custom = map[string]string{
			"permissions": string(permJSON),
		}
	}

	accessToken, err := h.jwtService.GenerateAccessToken(claims, AccessTokenDuration)
	if err != nil {
		return "", "", err
	}

	refreshToken, err := h.jwtService.GenerateRefreshToken(user.ID, RefreshTokenDuration)
	if err != nil {
		return "", "", err
	}

	return accessToken, refreshToken, nil
}

func generateRandomState() (string, error) {
	b := make([]byte, 32)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.URLEncoding.EncodeToString(b), nil
}

func deref(s *string) string {
	if s == nil {
		return ""
	}
	return *s
}
