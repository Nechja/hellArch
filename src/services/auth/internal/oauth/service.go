package oauth

import (
	"context"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"fmt"
	"net/http"

	"github.com/nechja/hellarch/auth/internal/config"
	"golang.org/x/oauth2"
)

type Provider string

const (
	ProviderDiscord   Provider = "discord"
	ProviderGoogle    Provider = "google"
	ProviderGitHub    Provider = "github"
	ProviderApple     Provider = "apple"
	ProviderMicrosoft Provider = "microsoft"
	ProviderTwitch    Provider = "twitch"
	ProviderHellArch  Provider = "hellarch"
)

type UserInfo struct {
	ID        string
	Email     string
	Username  string
	AvatarURL string
	Provider  Provider
}

type UserInfoFetcher func(ctx context.Context, token *oauth2.Token) (*UserInfo, error)

type HTTPClient interface {
	Do(req *http.Request) (*http.Response, error)
}

type Service struct {
	configs          map[Provider]*oauth2.Config
	userInfoFetchers map[Provider]UserInfoFetcher
	appleConfig      *config.AppleConfig
	hellarchBaseURL  string
	httpClient       HTTPClient
	appleJWKSCache   *jwksCache
}

func NewService(cfg config.OAuthConfig, httpClient HTTPClient) *Service {
	if httpClient == nil {
		httpClient = http.DefaultClient
	}
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

	var appleConfig *config.AppleConfig
	if cfg.Apple.ClientID != "" {
		appleConfig = &cfg.Apple
		configs[ProviderApple] = &oauth2.Config{
			ClientID:    cfg.Apple.ClientID,
			RedirectURL: cfg.Apple.RedirectURI,
			Scopes:      []string{"name", "email"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://appleid.apple.com/auth/authorize",
				TokenURL: "https://appleid.apple.com/auth/token",
			},
		}
	}

	if cfg.Microsoft.ClientID != "" {
		configs[ProviderMicrosoft] = &oauth2.Config{
			ClientID:     cfg.Microsoft.ClientID,
			ClientSecret: cfg.Microsoft.ClientSecret,
			RedirectURL:  cfg.Microsoft.RedirectURI,
			Scopes:       []string{"openid", "email", "profile", "User.Read"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
				TokenURL: "https://login.microsoftonline.com/common/oauth2/v2.0/token",
			},
		}
	}

	if cfg.Twitch.ClientID != "" {
		configs[ProviderTwitch] = &oauth2.Config{
			ClientID:     cfg.Twitch.ClientID,
			ClientSecret: cfg.Twitch.ClientSecret,
			RedirectURL:  cfg.Twitch.RedirectURI,
			Scopes:       []string{"user:read:email"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  "https://id.twitch.tv/oauth2/authorize",
				TokenURL: "https://id.twitch.tv/oauth2/token",
			},
		}
	}

	var hellarchBaseURL string
	if cfg.HellArch.ClientID != "" {
		hellarchBaseURL = cfg.HellArch.IdentityURL
		// AuthURL uses public URL (browser redirect), TokenURL uses internal URL (server-to-server)
		publicURL := cfg.HellArch.IdentityPublicURL
		if publicURL == "" {
			publicURL = hellarchBaseURL
		}
		configs[ProviderHellArch] = &oauth2.Config{
			ClientID:     cfg.HellArch.ClientID,
			ClientSecret: cfg.HellArch.ClientSecret,
			RedirectURL:  cfg.HellArch.RedirectURI,
			Scopes:       []string{"openid", "profile", "email"},
			Endpoint: oauth2.Endpoint{
				AuthURL:  publicURL + "/api/identity/authorize",
				TokenURL: hellarchBaseURL + "/identity/token",
			},
		}
	}

	svc := &Service{
		configs:          configs,
		userInfoFetchers: make(map[Provider]UserInfoFetcher),
		appleConfig:      appleConfig,
		hellarchBaseURL:  hellarchBaseURL,
		httpClient:       httpClient,
		appleJWKSCache:   &jwksCache{},
	}

	svc.userInfoFetchers[ProviderDiscord] = svc.getDiscordUserInfo
	svc.userInfoFetchers[ProviderGoogle] = svc.getGoogleUserInfo
	svc.userInfoFetchers[ProviderGitHub] = svc.getGitHubUserInfo
	svc.userInfoFetchers[ProviderApple] = svc.getAppleUserInfo
	svc.userInfoFetchers[ProviderMicrosoft] = svc.getMicrosoftUserInfo
	svc.userInfoFetchers[ProviderTwitch] = svc.getTwitchUserInfo
	svc.userInfoFetchers[ProviderHellArch] = svc.getHellArchUserInfo

	return svc
}

// GeneratePKCEVerifier creates a cryptographically random PKCE code verifier
func GeneratePKCEVerifier() (string, error) {
	b := make([]byte, 32)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(b), nil
}

// generatePKCEChallenge creates S256 challenge from verifier
func generatePKCEChallenge(verifier string) string {
	h := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(h[:])
}

func (s *Service) GetAuthURL(provider Provider, state string, pkceVerifier string) (string, error) {
	cfg, ok := s.configs[provider]
	if !ok {
		return "", fmt.Errorf("provider %s not configured", provider)
	}

	opts := []oauth2.AuthCodeOption{}
	if pkceVerifier != "" {
		challenge := generatePKCEChallenge(pkceVerifier)
		opts = append(opts,
			oauth2.SetAuthURLParam("code_challenge", challenge),
			oauth2.SetAuthURLParam("code_challenge_method", "S256"),
		)
	}

	return cfg.AuthCodeURL(state, opts...), nil
}

func (s *Service) Exchange(ctx context.Context, provider Provider, code string, pkceVerifier string) (*oauth2.Token, error) {
	cfg, ok := s.configs[provider]
	if !ok {
		return nil, fmt.Errorf("provider %s not configured", provider)
	}

	if provider == ProviderApple && s.appleConfig != nil {
		clientSecret, err := generateAppleClientSecret(s.appleConfig)
		if err != nil {
			return nil, fmt.Errorf("failed to generate apple client secret: %w", err)
		}
		cfg.ClientSecret = clientSecret
	}

	opts := []oauth2.AuthCodeOption{}
	if pkceVerifier != "" {
		opts = append(opts, oauth2.SetAuthURLParam("code_verifier", pkceVerifier))
	}

	return cfg.Exchange(ctx, code, opts...)
}

func (s *Service) GetUserInfo(ctx context.Context, provider Provider, token *oauth2.Token) (*UserInfo, error) {
	fetcher, ok := s.userInfoFetchers[provider]
	if !ok {
		return nil, fmt.Errorf("unsupported provider: %s", provider)
	}
	return fetcher(ctx, token)
}

func (s *Service) IsProviderConfigured(provider Provider) bool {
	_, ok := s.configs[provider]
	return ok
}

func (s *Service) GetConfiguredProviders() []Provider {
	providers := make([]Provider, 0, len(s.configs))
	for p := range s.configs {
		providers = append(providers, p)
	}
	return providers
}