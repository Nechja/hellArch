package handler

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"encoding/json"
	"errors"
	"log/slog"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/events"
	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

const (
	AccessTokenDuration  = 15 * time.Minute
	RefreshTokenDuration = 7 * 24 * time.Hour
	SessionDuration      = 30 * 24 * time.Hour
	OAuthStateTTL        = 10 * time.Minute
)

type AuthHandler struct {
	postgres     *store.Postgres
	redis        *store.Redis
	jwtService   *jwt.Service
	oauthService *oauth.Service
	publisher    *events.Publisher
	config       *config.Config
}

func NewAuthHandler(
	postgres *store.Postgres,
	redis *store.Redis,
	jwtService *jwt.Service,
	oauthService *oauth.Service,
	publisher *events.Publisher,
	cfg *config.Config,
) *AuthHandler {
	return &AuthHandler{
		postgres:     postgres,
		redis:        redis,
		jwtService:   jwtService,
		oauthService: oauthService,
		publisher:    publisher,
		config:       cfg,
	}
}

// RegisterAuthServiceServer registers a placeholder - actual proto implementation needed
func RegisterAuthServiceServer(s *grpc.Server, h *AuthHandler) {
	// Proto-generated code will register the actual service
	// For now, this is a placeholder that will be replaced when protos are generated
	slog.Info("auth handler registered (proto registration pending)")
}

// GetOAuthURL generates an OAuth authorization URL for the specified provider
func (h *AuthHandler) GetOAuthURL(ctx context.Context, provider oauth.Provider, redirectURI string) (string, string, error) {
	state, err := generateRandomState()
	if err != nil {
		return "", "", status.Error(codes.Internal, "failed to generate state")
	}

	stateData, _ := json.Marshal(map[string]string{
		"redirect_uri": redirectURI,
		"created_at":   time.Now().Format(time.RFC3339),
	})
	if err := h.redis.SetOAuthState(ctx, state, string(stateData), OAuthStateTTL); err != nil {
		return "", "", status.Error(codes.Internal, "failed to store state")
	}

	authURL, err := h.oauthService.GetAuthURL(provider, state)
	if err != nil {
		return "", "", status.Error(codes.InvalidArgument, err.Error())
	}

	return authURL, state, nil
}

// HandleOAuthCallback processes the OAuth callback and creates/updates user
func (h *AuthHandler) HandleOAuthCallback(ctx context.Context, provider oauth.Provider, code, state, ipAddress, userAgent string) (*AuthResult, error) {
	stateData, err := h.redis.GetOAuthState(ctx, state)
	if err != nil {
		return nil, status.Error(codes.InvalidArgument, "invalid or expired state")
	}
	_ = h.redis.DeleteOAuthState(ctx, state)

	var stateInfo map[string]string
	if err := json.Unmarshal([]byte(stateData), &stateInfo); err != nil {
		return nil, status.Error(codes.Internal, "failed to parse state")
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

	user, isNewUser, err := h.findOrCreateUser(ctx, userInfo)
	if err != nil {
		slog.Error("failed to find or create user", "error", err)
		return nil, status.Error(codes.Internal, "failed to create user")
	}

	if isNewUser {
		if err := h.publisher.PublishUserCreated(ctx, events.UserCreatedData{
			UserID:         user.ID,
			Email:          user.Email,
			Username:       userInfo.Username,
			Provider:       string(provider),
			ProviderUserID: userInfo.ID,
			AvatarURL:      userInfo.AvatarURL,
			CreatedAt:      user.CreatedAt.Format(time.RFC3339),
		}); err != nil {
			slog.Error("failed to publish user.created event", "error", err, "user_id", user.ID)
		}
	}

	accessToken, refreshToken, err := h.generateTokens(user, userInfo)
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to generate tokens")
	}

	sessionToken, err := h.createSession(ctx, user.ID, ipAddress, userAgent)
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to create session")
	}

	return &AuthResult{
		AccessToken:  accessToken,
		RefreshToken: refreshToken,
		SessionToken: sessionToken,
		ExpiresIn:    int64(AccessTokenDuration.Seconds()),
		User: &UserInfo{
			ID:        user.ID,
			Email:     user.Email,
			Username:  userInfo.Username,
			AvatarURL: userInfo.AvatarURL,
			Provider:  string(provider),
		},
		RedirectURI: stateInfo["redirect_uri"],
	}, nil
}

func (h *AuthHandler) findOrCreateUser(ctx context.Context, info *oauth.UserInfo) (*store.User, bool, error) {
	user, err := h.postgres.GetUserByProviderID(ctx, string(info.Provider), info.ID)
	if err == nil {
		return user, false, nil
	}

	if !errors.Is(err, pgx.ErrNoRows) {
		return nil, false, err
	}

	user, err = h.postgres.CreateUser(ctx, info.Email, info.Username)
	if err != nil {
		return nil, false, err
	}

	err = h.postgres.CreateLinkedAccount(ctx, &store.LinkedAccount{
		UserID:         user.ID,
		Provider:       string(info.Provider),
		ProviderUserID: info.ID,
		Email:          ptr(info.Email),
		Username:       ptr(info.Username),
		AvatarURL:      ptr(info.AvatarURL),
	})
	if err != nil {
		return nil, false, err
	}

	return user, true, nil
}

func (h *AuthHandler) generateTokens(user *store.User, info *oauth.UserInfo) (string, string, error) {
	claims := &jwt.Claims{
		UserID:   user.ID,
		Email:    user.Email,
		Username: info.Username,
		Provider: string(info.Provider),
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

func (h *AuthHandler) createSession(ctx context.Context, userID, ipAddress, userAgent string) (string, error) {
	sessionToken, err := generateRandomState()
	if err != nil {
		return "", err
	}

	sessionID := uuid.New().String()

	session := &store.Session{
		ID:        sessionID,
		UserID:    userID,
		Token:     sessionToken,
		IPAddress: ptrOrNil(ipAddress),
		UserAgent: ptrOrNil(userAgent),
		ExpiresAt: time.Now().Add(SessionDuration),
	}

	if err := h.postgres.CreateSession(ctx, session); err != nil {
		return "", err
	}

	if err := h.redis.CacheSession(ctx, sessionToken, userID, SessionDuration); err != nil {
		slog.Warn("failed to cache session in redis", "error", err)
	}

	return sessionToken, nil
}

// ValidateSession validates a session token and returns the user ID
func (h *AuthHandler) ValidateSession(ctx context.Context, sessionToken string) (string, error) {
	userID, err := h.redis.GetCachedSession(ctx, sessionToken)
	if err == nil {
		return userID, nil
	}

	session, err := h.postgres.GetSessionByToken(ctx, sessionToken)
	if err != nil {
		return "", status.Error(codes.Unauthenticated, "invalid session")
	}

	_ = h.redis.CacheSession(ctx, sessionToken, session.UserID, time.Until(session.ExpiresAt))

	return session.UserID, nil
}

// RefreshToken exchanges a refresh token for new access and refresh tokens
func (h *AuthHandler) RefreshToken(ctx context.Context, refreshToken string) (string, string, error) {
	claims, err := h.jwtService.ValidateToken(refreshToken)
	if err != nil {
		return "", "", status.Error(codes.Unauthenticated, "invalid refresh token")
	}

	user, err := h.postgres.GetUserByID(ctx, claims.Subject)
	if err != nil {
		return "", "", status.Error(codes.NotFound, "user not found")
	}

	accounts, err := h.postgres.GetLinkedAccounts(ctx, user.ID)
	if err != nil || len(accounts) == 0 {
		return "", "", status.Error(codes.Internal, "no linked accounts")
	}

	newClaims := &jwt.Claims{
		UserID:   user.ID,
		Email:    user.Email,
		Username: deref(accounts[0].Username),
		Provider: accounts[0].Provider,
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

// ValidateToken validates a JWT and returns the claims
func (h *AuthHandler) ValidateToken(ctx context.Context, token string) (*jwt.Claims, error) {
	return h.jwtService.ValidateToken(token)
}

// AuthResult represents the result of a successful authentication
type AuthResult struct {
	AccessToken  string
	RefreshToken string
	SessionToken string
	ExpiresIn    int64
	User         *UserInfo
	RedirectURI  string
}

// UserInfo represents user information returned after authentication
type UserInfo struct {
	ID        string
	Email     string
	Username  string
	AvatarURL string
	Provider  string
}

func generateRandomState() (string, error) {
	b := make([]byte, 32)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.URLEncoding.EncodeToString(b), nil
}

// ptr returns a pointer to the given string
func ptr(s string) *string {
	return &s
}

// ptrOrNil returns nil if the string is empty, otherwise a pointer to it
func ptrOrNil(s string) *string {
	if s == "" {
		return nil
	}
	return &s
}

// deref returns the string value or empty string if nil
func deref(s *string) string {
	if s == nil {
		return ""
	}
	return *s
}
