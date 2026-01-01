package handler

import "time"

const (
	AccessTokenDuration  = 15 * time.Minute
	RefreshTokenDuration = 7 * 24 * time.Hour
	SessionDuration      = 24 * time.Hour
	OAuthStateTTL        = 10 * time.Minute
	MaxSessionsPerUser   = 5
)

type AuthResult struct {
	AccessToken  string
	RefreshToken string
	SessionToken string
	ExpiresIn    int64
	User         *UserInfo
	RedirectURI  string
}

type UserInfo struct {
	ID        string
	Email     string
	Username  string
	AvatarURL string
	Provider  string
}
