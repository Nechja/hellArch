package oauth

import (
	"context"
	"fmt"

	"github.com/nechja/hellarch/auth/internal/config"
	"golang.org/x/oauth2"
)

type Provider string

const (
	ProviderDiscord Provider = "discord"
	ProviderGoogle  Provider = "google"
	ProviderGitHub  Provider = "github"
)

type UserInfo struct {
	ID        string
	Email     string
	Username  string
	AvatarURL string
	Provider  Provider
}

type Service struct {
	configs map[Provider]*oauth2.Config
}

func NewService(cfg config.OAuthConfig) *Service {
	configs := make(map[Provider]*oauth2.Config)

	if cfg.Discord.ClientID != "" {
		configs[ProviderDiscord] = &oauth2.Config{
			ClientID:     cfg.Discord.ClientID,
			ClientSecret: cfg.Discord.ClientSecret,
			RedirectURL:  cfg.Discord.RedirectURI,
			Scopes:       []string{"identify", "email"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://discord.com/api/oauth2/authorize",
				TokenURL: "https://discord.com/api/oauth2/token",
			},
		}
	}

	if cfg.Google.ClientID != "" {
		configs[ProviderGoogle] = &oauth2.Config{
			ClientID:     cfg.Google.ClientID,
			ClientSecret: cfg.Google.ClientSecret,
			RedirectURL:  cfg.Google.RedirectURI,
			Scopes:       []string{"openid", "email", "profile"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://accounts.google.com/o/oauth2/v2/auth",
				TokenURL: "https://oauth2.googleapis.com/token",
			},
		}
	}

	if cfg.GitHub.ClientID != "" {
		configs[ProviderGitHub] = &oauth2.Config{
			ClientID:     cfg.GitHub.ClientID,
			ClientSecret: cfg.GitHub.ClientSecret,
			RedirectURL:  cfg.GitHub.RedirectURI,
			Scopes:       []string{"read:user", "user:email"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://github.com/login/oauth/authorize",
				TokenURL: "https://github.com/login/oauth/access_token",
			},
		}
	}

	return &Service{configs: configs}
}

func (s *Service) GetAuthURL(provider Provider, state string) (string, error) {
	cfg, ok := s.configs[provider]
	if !ok {
		return "", fmt.Errorf("provider %s not configured", provider)
	}
	return cfg.AuthCodeURL(state), nil
}

func (s *Service) Exchange(ctx context.Context, provider Provider, code string) (*oauth2.Token, error) {
	cfg, ok := s.configs[provider]
	if !ok {
		return nil, fmt.Errorf("provider %s not configured", provider)
	}
	return cfg.Exchange(ctx, code)
}

func (s *Service) GetUserInfo(ctx context.Context, provider Provider, token *oauth2.Token) (*UserInfo, error) {
	switch provider {
	case ProviderDiscord:
		return s.getDiscordUserInfo(ctx, token)
	case ProviderGoogle:
		return s.getGoogleUserInfo(ctx, token)
	case ProviderGitHub:
		return s.getGitHubUserInfo(ctx, token)
	default:
		return nil, fmt.Errorf("unsupported provider: %s", provider)
	}
}

func (s *Service) IsProviderConfigured(provider Provider) bool {
	_, ok := s.configs[provider]
	return ok
}