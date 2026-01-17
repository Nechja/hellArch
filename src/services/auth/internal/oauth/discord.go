package oauth

import (
	"context"
	"fmt"

	"golang.org/x/oauth2"
)

type discordUser struct {
	ID            string `json:"id"`
	Username      string `json:"username"`
	Discriminator string `json:"discriminator"`
	Email         string `json:"email"`
	Avatar        string `json:"avatar"`
}

func (s *Service) getDiscordUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	data, err := fetchUserData[discordUser](ctx, s.httpClient, token, "https://discord.com/api/users/@me", nil)
	if err != nil {
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
