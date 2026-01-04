package handler

import "time"

const (
	AccessTokenDuration  = 15 * time.Minute
	RefreshTokenDuration = 7 * 24 * time.Hour
	OAuthStateTTL        = 10 * time.Minute
	// MaxSessionsPerUser and SessionDuration are now configurable via environment variables:
	// - MAX_SESSIONS_PER_USER (default: 5)
	// - SESSION_DURATION_HOURS (default: 24)
)

type AuthResult struct {
	AccessToken  string
	RefreshToken string
	SessionToken string
	ExpiresIn    int64
	User         *UserInfo
	RedirectURI  string
	WasLinked    bool
}

type UserInfo struct {
	ID        string
	Email     string
	Username  string
	AvatarURL string
	Provider  string
}
