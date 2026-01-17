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
}

func NewTokenHandler(jwtService *jwt.Service, userService *service.User) *TokenHandler {
	return &TokenHandler{
		jwtService:  jwtService,
		userService: userService,
	}
}

func (h *TokenHandler) Refresh(ctx context.Context, refreshToken string) (string, string, error) {
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

	return accessToken, newRefreshToken, nil
}

func (h *TokenHandler) Validate(ctx context.Context, token string) (*jwt.Claims, error) {
	return h.jwtService.ValidateToken(token)
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
