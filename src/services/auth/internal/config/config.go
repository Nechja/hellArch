package config

import (
	"fmt"
	"os"
)

// Config holds all application configuration values.
type Config struct {
	Environment       string
	GRPCAddress       string
	HTTPAddress       string
	DatabaseURL       string
	RedisURL          string
	RabbitMQURL       string
	JWTPrivateKeyPath string
	JWTPublicKeyPath  string
	OAuth             OAuthConfig
}

// OAuthConfig holds OAuth provider configurations.
type OAuthConfig struct {
	Discord ProviderConfig
	Google  ProviderConfig
	GitHub  ProviderConfig
}

// ProviderConfig holds credentials for a single OAuth provider.
type ProviderConfig struct {
	ClientID     string
	ClientSecret string
	RedirectURI  string
}

func Load() (*Config, error) {
	cfg := &Config{
		Environment:       getEnv("HELLARCH_ENV", "development"),
		GRPCAddress:       getEnv("GRPC_ADDRESS", ":50051"),
		HTTPAddress:       getEnv("HTTP_ADDRESS", ":5001"),
		DatabaseURL:       getEnv("AUTH_DATABASE_URL", "postgres://hellarch:hellarch@localhost:5432/auth_db"),
		RedisURL:          getEnv("REDIS_URL", "redis://localhost:6379"),
		RabbitMQURL:       getEnv("RABBITMQ_URL", "amqp://hellarch:hellarch@localhost:5672"),
		JWTPrivateKeyPath: getEnv("JWT_PRIVATE_KEY_PATH", "./infra/keys/dev-private.pem"),
		JWTPublicKeyPath:  getEnv("JWT_PUBLIC_KEY_PATH", "./infra/keys/dev-public.pem"),
		OAuth: OAuthConfig{
			Discord: ProviderConfig{
				ClientID:     os.Getenv("DISCORD_CLIENT_ID"),
				ClientSecret: os.Getenv("DISCORD_CLIENT_SECRET"),
				RedirectURI:  getEnv("DISCORD_REDIRECT_URI", "http://localhost:5000/api/auth/discord/callback"),
			},
			Google: ProviderConfig{
				ClientID:     os.Getenv("GOOGLE_CLIENT_ID"),
				ClientSecret: os.Getenv("GOOGLE_CLIENT_SECRET"),
				RedirectURI:  getEnv("GOOGLE_REDIRECT_URI", "http://localhost:5000/api/auth/google/callback"),
			},
			GitHub: ProviderConfig{
				ClientID:     os.Getenv("GITHUB_CLIENT_ID"),
				ClientSecret: os.Getenv("GITHUB_CLIENT_SECRET"),
				RedirectURI:  getEnv("GITHUB_REDIRECT_URI", "http://localhost:5000/api/auth/github/callback"),
			},
		},
	}

	if err := cfg.validate(); err != nil {
		return nil, err
	}

	return cfg, nil
}

func (c *Config) validate() error {
	if c.OAuth.Discord.ClientID == "" {
		return fmt.Errorf("DISCORD_CLIENT_ID is required")
	}
	if c.OAuth.Discord.ClientSecret == "" {
		return fmt.Errorf("DISCORD_CLIENT_SECRET is required")
	}
	return nil
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}
