package oauth

import (
	"context"
	"fmt"

	"golang.org/x/oauth2"
)

type githubUser struct {
	ID        int    `json:"id"`
	Login     string `json:"login"`
	Email     string `json:"email"`
	AvatarURL string `json:"avatar_url"`
}

func (s *Service) getGitHubUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	data, err := fetchUserData[githubUser](ctx, token, "https://api.github.com/user", map[string]string{
		"Accept": "application/vnd.github.v3+json",
	})
	if err != nil {
		return nil, err
	}

	return &UserInfo{
		ID:        fmt.Sprintf("%d", data.ID),
		Email:     data.Email,
		Username:  data.Login,
		AvatarURL: data.AvatarURL,
		Provider:  ProviderGitHub,
	}, nil
}
