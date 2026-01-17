package handler

import (
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"errors"
	"log/slog"
	"net/url"
	"strings"
	"time"

	"github.com/nechja/hellarch/auth/internal/config"
)

type SecurityHandler struct {
	config *config.Config
}

func NewSecurityHandler(cfg *config.Config) *SecurityHandler {
	return &SecurityHandler{
		config: cfg,
	}
}

func (h *SecurityHandler) ValidateRedirectURI(redirectURI string) error {
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

func (h *SecurityHandler) ValidateCSRFToken(csrfToken, storedHash string) error {
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

func (h *SecurityHandler) HashCSRFToken(csrfToken string) string {
	hash := sha256.Sum256([]byte(csrfToken))
	return base64.URLEncoding.EncodeToString(hash[:])
}

func (h *SecurityHandler) OAuthStateTTL() time.Duration {
	return h.config.OAuthStateTTL
}

func GenerateRandomState() (string, error) {
	b := make([]byte, 32)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.URLEncoding.EncodeToString(b), nil
}
