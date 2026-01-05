package config

import (
	"fmt"
	"os"
	"time"

	sharedconfig "github.com/nechja/hellarch/golibs/config"
)

type Config struct {
	Environment         string
	GRPCAddress         string
	HTTPAddress         string
	DatabaseURL         string
	RedisURL            string
	RabbitMQURL         string
	JWTPrivateKeyPath   string
	JWTPublicKeyPath    string
	OAuth               OAuthConfig
	AllowedRedirectURIs []string
	SecureCookies       bool
	MaxSessionsPerUser  int
	SessionDuration     time.Duration
}

type OAuthConfig struct {
	Discord   ProviderConfig
	Google    ProviderConfig
	GitHub    ProviderConfig
	Apple     AppleConfig
	Microsoft ProviderConfig
	Twitch    ProviderConfig
	HellArch  HellArchConfig
}

type HellArchConfig struct {
	ClientID          string
	ClientSecret      string
	RedirectURI       string
	IdentityURL       string // Internal URL for server-to-server (token, userinfo)
	IdentityPublicURL string // Public URL for browser redirects (authorize)
}

type ProviderConfig struct {
	ClientID     string
	ClientSecret string
	RedirectURI  string
}

type AppleConfig struct {
	ClientID       string
	TeamID         string
	KeyID          string
	PrivateKeyPath string
	RedirectURI    string
}

func Load() (*Config, error) {
	cfg := &Config{
		Environment:         sharedconfig.GetEnv("HELLARCH_ENV", "development"),
		GRPCAddress:         sharedconfig.GetEnv("GRPC_ADDRESS", ":50051"),
		HTTPAddress:         sharedconfig.GetEnv("HTTP_ADDRESS", ":5001"),
		DatabaseURL:         sharedconfig.GetEnv("AUTH_DATABASE_URL", "postgres://hellarch:hellarch@localhost:5432/auth_db"),
		RedisURL:            sharedconfig.GetEnv("REDIS_URL", "redis://localhost:6379"),
		RabbitMQURL:         sharedconfig.GetEnv("RABBITMQ_URL", "amqp://hellarch:hellarch@localhost:5672"),
		JWTPrivateKeyPath:   sharedconfig.GetEnv("JWT_PRIVATE_KEY_PATH", "./infra/keys/dev-private.pem"),
		JWTPublicKeyPath:    sharedconfig.GetEnv("JWT_PUBLIC_KEY_PATH", "./infra/keys/dev-public.pem"),
		AllowedRedirectURIs: sharedconfig.GetEnvList("ALLOWED_REDIRECT_URIS", "http://localhost:5000,http://localhost:5003"),
		SecureCookies:       sharedconfig.GetEnvBool("SECURE_COOKIES", false),
		MaxSessionsPerUser:  sharedconfig.GetEnvInt("MAX_SESSIONS_PER_USER", 5),
		SessionDuration:     time.Duration(sharedconfig.GetEnvInt("SESSION_DURATION_HOURS", 24)) * time.Hour,
		OAuth: OAuthConfig{
			Discord: ProviderConfig{
				ClientID:     os.Getenv("DISCORD_CLIENT_ID"),
				ClientSecret: os.Getenv("DISCORD_CLIENT_SECRET"),
				RedirectURI:  sharedconfig.GetEnv("DISCORD_REDIRECT_URI", "http://localhost:5000/api/auth/discord/callback"),
			},
			Google: ProviderConfig{
				ClientID:     os.Getenv("GOOGLE_CLIENT_ID"),
				ClientSecret: os.Getenv("GOOGLE_CLIENT_SECRET"),
				RedirectURI:  sharedconfig.GetEnv("GOOGLE_REDIRECT_URI", "http://localhost:5000/api/auth/google/callback"),
			},
			GitHub: ProviderConfig{
				ClientID:     os.Getenv("GITHUB_CLIENT_ID"),
				ClientSecret: os.Getenv("GITHUB_CLIENT_SECRET"),
				RedirectURI:  sharedconfig.GetEnv("GITHUB_REDIRECT_URI", "http://localhost:5000/api/auth/github/callback"),
			},
			Apple: AppleConfig{
				ClientID:       os.Getenv("APPLE_CLIENT_ID"),
				TeamID:         os.Getenv("APPLE_TEAM_ID"),
				KeyID:          os.Getenv("APPLE_KEY_ID"),
				PrivateKeyPath: os.Getenv("APPLE_PRIVATE_KEY_PATH"),
				RedirectURI:    sharedconfig.GetEnv("APPLE_REDIRECT_URI", "http://localhost:5000/api/auth/apple/callback"),
			},
			Microsoft: ProviderConfig{
				ClientID:     os.Getenv("MICROSOFT_CLIENT_ID"),
				ClientSecret: os.Getenv("MICROSOFT_CLIENT_SECRET"),
				RedirectURI:  sharedconfig.GetEnv("MICROSOFT_REDIRECT_URI", "http://localhost:5000/api/auth/microsoft/callback"),
			},
			Twitch: ProviderConfig{
				ClientID:     os.Getenv("TWITCH_CLIENT_ID"),
				ClientSecret: os.Getenv("TWITCH_CLIENT_SECRET"),
				RedirectURI:  sharedconfig.GetEnv("TWITCH_REDIRECT_URI", "http://localhost:5000/api/auth/twitch/callback"),
			},
			HellArch: HellArchConfig{
				ClientID:          os.Getenv("HELLARCH_CLIENT_ID"),
				ClientSecret:      os.Getenv("HELLARCH_CLIENT_SECRET"),
				RedirectURI:       sharedconfig.GetEnv("HELLARCH_REDIRECT_URI", "http://localhost:5000/api/auth/hellarch/callback"),
				IdentityURL:       sharedconfig.GetEnv("HELLARCH_IDENTITY_URL", "http://localhost:5010"),
				IdentityPublicURL: sharedconfig.GetEnv("HELLARCH_IDENTITY_PUBLIC_URL", "http://localhost:5000"),
			},
		},
	}

	if err := cfg.validate(); err != nil {
		return nil, err
	}

	return cfg, nil
}

func (c *Config) validate() error {
	hasProvider := c.OAuth.Discord.ClientID != "" ||
		c.OAuth.Google.ClientID != "" ||
		c.OAuth.GitHub.ClientID != "" ||
		c.OAuth.Apple.ClientID != "" ||
		c.OAuth.Microsoft.ClientID != "" ||
		c.OAuth.Twitch.ClientID != "" ||
		c.OAuth.HellArch.ClientID != ""

	if !hasProvider {
		return fmt.Errorf("at least one OAuth provider must be configured")
	}
	return nil
}
