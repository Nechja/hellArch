package oauth

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"

	"golang.org/x/oauth2"
)

func (s *Service) getDiscordUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, "https://discord.com/api/users/@me", nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("Authorization", "Bearer "+token.AccessToken)

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer func() { _ = resp.Body.Close() }()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	var data struct {
		ID            string `json:"id"`
		Username      string `json:"username"`
		Discriminator string `json:"discriminator"`
		Email         string `json:"email"`
		Avatar        string `json:"avatar"`
	}
	if err := json.Unmarshal(body, &data); err != nil {
		return nil, err
	}

	avatarURL := ""
	if data.Avatar != "" {
		avatarURL = fmt.Sprintf("https://cdn.discordapp.com/avatars/%s/%s.png", data.ID, data.Avatar)
	}

	return &UserInfo{
		ID:        data.ID,
		Email:     data.Email,
		Username:  data.Username,
		AvatarURL: avatarURL,
		Provider:  ProviderDiscord,
	}, nil
}
