package service

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"time"

	"github.com/google/uuid"
	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type Session struct {
	redis  *store.Redis
	config *config.Config
}

func NewSession(redis *store.Redis, cfg *config.Config) *Session {
	return &Session{
		redis:  redis,
		config: cfg,
	}
}

func (s *Session) Create(ctx context.Context, userID, ipAddress, userAgent string) (string, error) {
	if err := s.redis.EnforceMaxSessions(ctx, userID, s.config.MaxSessionsPerUser); err != nil {
		// Log but don't fail - session creation is more important
		_ = err
	}

	token, err := generateSecureToken()
	if err != nil {
		return "", status.Error(codes.Internal, "failed to generate session token")
	}

	session := &store.RedisSession{
		ID:        uuid.New().String(),
		UserID:    userID,
		IPAddress: ipAddress,
		UserAgent: userAgent,
		CreatedAt: time.Now(),
		ExpiresAt: time.Now().Add(s.config.SessionDuration),
	}

	if err := s.redis.CreateSession(ctx, token, session, s.config.SessionDuration); err != nil {
		return "", status.Error(codes.Internal, "failed to create session")
	}

	return token, nil
}

func (s *Session) Validate(ctx context.Context, token string) (*store.RedisSession, error) {
	session, err := s.redis.GetSession(ctx, token)
	if err != nil {
		return nil, status.Error(codes.Unauthenticated, "invalid session")
	}
	return session, nil
}

func (s *Session) Invalidate(ctx context.Context, token string) error {
	return s.redis.DeleteSession(ctx, token)
}

func (s *Session) InvalidateByID(ctx context.Context, sessionID string) error {
	return s.redis.DeleteSessionByID(ctx, sessionID)
}

func (s *Session) InvalidateAll(ctx context.Context, userID string) error {
	return s.redis.DeleteUserSessions(ctx, userID)
}

func (s *Session) List(ctx context.Context, userID string) ([]*store.RedisSession, error) {
	return s.redis.GetUserSessions(ctx, userID)
}

func (s *Session) CountActive(ctx context.Context) (int, error) {
	return s.redis.CountActiveSessions(ctx)
}

func generateSecureToken() (string, error) {
	b := make([]byte, 32)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.URLEncoding.EncodeToString(b), nil
}
