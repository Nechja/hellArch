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
	ServiceAccounts     map[string]string // service_id -> secret for internal auth
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
		ServiceAccounts:     loadServiceAccounts(),
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

func loadServiceAccounts() map[string]string {
	accounts := make(map[string]string)

	// Load from SERVICE_ACCOUNTS env var (format: "service1:secret1,service2:secret2")
	raw := os.Getenv("SERVICE_ACCOUNTS")
	if raw != "" {
		for _, pair := range splitAndTrim(raw, ",") {
			parts := splitAndTrim(pair, ":")
			if len(parts) == 2 {
				accounts[parts[0]] = parts[1]
			}
		}
	}

	// Default dev service accounts
	if len(accounts) == 0 {
		accounts["testing-service"] = "testing-secret"
		accounts["health-service"] = "health-secret"
	}

	return accounts
}

func splitAndTrim(s, sep string) []string {
	var result []string
	for _, part := range sharedconfig.GetEnvList(s, "") {
		result = append(result, part)
	}
	if len(result) == 0 && s != "" {
		// Manual split for raw string
		for _, p := range split(s, sep) {
			if trimmed := trim(p); trimmed != "" {
				result = append(result, trimmed)
			}
		}
	}
	return result
}

func split(s, sep string) []string {
	var result []string
	start := 0
	for i := 0; i <= len(s)-len(sep); i++ {
		if s[i:i+len(sep)] == sep {
			result = append(result, s[start:i])
			start = i + len(sep)
		}
	}
	result = append(result, s[start:])
	return result
}

func trim(s string) string {
	start := 0
	end := len(s)
	for start < end && (s[start] == ' ' || s[start] == '\t') {
		start++
	}
	for end > start && (s[end-1] == ' ' || s[end-1] == '\t') {
		end--
	}
	return s[start:end]
}
