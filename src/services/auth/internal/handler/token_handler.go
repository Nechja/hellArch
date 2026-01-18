package handler

import (
	"context"
	"encoding/json"
	"log/slog"
	"time"

	"github.com/nechja/hellarch/auth/internal/jwt"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/service"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type TokenHandler struct {
	jwtService  *jwt.Service
	userService *service.User
	redis       *store.Redis
}

func NewTokenHandler(jwtService *jwt.Service, userService *service.User, redis *store.Redis) *TokenHandler {
	return &TokenHandler{
		jwtService:  jwtService,
		userService: userService,
		redis:       redis,
	}
}

func (h *TokenHandler) Refresh(ctx context.Context, refreshToken string) (string, string, error) {
	claims, err := h.jwtService.ValidateToken(refreshToken)
	if err != nil {
		return "", "", status.Error(codes.Unauthenticated, "invalid refresh token")
	}

	// Check if the refresh token has been revoked (token rotation enforcement)
	if claims.ID != "" {
		revoked, err := h.redis.IsTokenRevoked(ctx, claims.ID)
		if err != nil {
			slog.Error("failed to check token revocation", "error", err)
			return "", "", status.Error(codes.Internal, "failed to validate token")
		}
		if revoked {
			slog.Warn("attempted use of revoked refresh token", "jti", claims.ID, "user_id", claims.Subject)
			return "", "", status.Error(codes.Unauthenticated, "token has been revoked")
		}
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

	if len(user.Permissions) > 0 {
		permJSON, err := json.Marshal(user.Permissions)
		if err != nil {
			slog.Error("failed to marshal user permissions", "userID", user.ID, "error", err)
			return "", "", status.Error(codes.Internal, "failed to process user permissions")
		}
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

	// Revoke the old refresh token to implement token rotation
	// This ensures each refresh token can only be used once
	if claims.ID != "" {
		if err := h.redis.RevokeToken(ctx, claims.ID, RefreshTokenDuration); err != nil {
			slog.Warn("failed to revoke old refresh token", "error", err, "jti", claims.ID)
			// Don't fail the request - the new token was already generated
		}
	}

	return accessToken, newRefreshToken, nil
}

func (h *TokenHandler) Validate(ctx context.Context, token string) (*jwt.Claims, error) {
	claims, err := h.jwtService.ValidateToken(token)
	if err != nil {
		return nil, err
	}

	// Check if the token has been revoked
	if claims.ID != "" {
		revoked, err := h.redis.IsTokenRevoked(ctx, claims.ID)
		if err != nil {
			slog.Error("failed to check token revocation", "error", err)
			return nil, status.Error(codes.Internal, "failed to validate token")
		}
		if revoked {
			return nil, status.Error(codes.Unauthenticated, "token has been revoked")
		}
	}

	return claims, nil
}

func (h *TokenHandler) GenerateServiceToken(serviceID string) (string, error) {
	claims := &jwt.Claims{
		UserID:   "service:" + serviceID,
		Username: serviceID,
		Custom: map[string]string{
			"type": "service",
		},
	}
	return h.jwtService.GenerateAccessToken(claims, time.Hour)
}

func (h *TokenHandler) GenerateTokenPair(user *store.User, info *oauth.UserInfo) (string, string, error) {
	claims := &jwt.Claims{
		UserID:   user.ID,
		Email:    user.Email,
		Username: info.Username,
		Provider: string(info.Provider),
	}

	if len(user.Permissions) > 0 {
		permJSON, err := json.Marshal(user.Permissions)
		if err != nil {
			slog.Error("failed to marshal user permissions", "userID", user.ID, "error", err)
			return "", "", status.Error(codes.Internal, "failed to process user permissions")
		}
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
