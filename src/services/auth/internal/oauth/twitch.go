package oauth

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"

	"golang.org/x/oauth2"
)

type twitchResponse struct {
	Data []twitchUser `json:"data"`
}

type twitchUser struct {
	ID              string `json:"id"`
	Login           string `json:"login"`
	DisplayName     string `json:"display_name"`
	Email           string `json:"email"`
	ProfileImageURL string `json:"profile_image_url"`
}

func (s *Service) getTwitchUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	cfg, ok := s.configs[ProviderTwitch]
	if !ok {
		return nil, fmt.Errorf("twitch provider not configured")
	}

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, "https://api.twitch.tv/helix/users", nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("Authorization", "Bearer "+token.AccessToken)
	req.Header.Set("Client-Id", cfg.ClientID)

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	var data twitchResponse
	if err := json.Unmarshal(body, &data); err != nil {
		return nil, err
	}

	if len(data.Data) == 0 {
		return nil, fmt.Errorf("no user data returned")
	}

	user := data.Data[0]
	return &UserInfo{
		ID:        user.ID,
		Email:     user.Email,
		Username:  user.DisplayName,
		AvatarURL: user.ProfileImageURL,
		Provider:  ProviderTwitch,
	}, nil
}
