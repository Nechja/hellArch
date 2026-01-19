package handler

import (
	"context"

	"github.com/nechja/hellarch/auth/internal/service"
	"github.com/nechja/hellarch/auth/internal/store"
)

type SessionHandler struct {
	sessionService *service.Session
}

func NewSessionHandler(sessionService *service.Session) *SessionHandler {
	return &SessionHandler{
		sessionService: sessionService,
	}
}

func (h *SessionHandler) Validate(ctx context.Context, sessionToken string) (string, error) {
	session, err := h.sessionService.Validate(ctx, sessionToken)
	if err != nil {
		return "", err
	}
	return session.UserID, nil
}

// GetSession returns the full session data including IP and UserAgent for binding validation
func (h *SessionHandler) GetSession(ctx context.Context, sessionToken string) (*store.RedisSession, error) {
	return h.sessionService.Validate(ctx, sessionToken)
}

func (h *SessionHandler) Invalidate(ctx context.Context, sessionToken string) error {
	return h.sessionService.Invalidate(ctx, sessionToken)
}

func (h *SessionHandler) Create(ctx context.Context, userID, ipAddress, userAgent string) (string, error) {
	return h.sessionService.Create(ctx, userID, ipAddress, userAgent)
}
