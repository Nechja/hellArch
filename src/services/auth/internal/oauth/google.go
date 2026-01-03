package oauth

import (
	"context"

	"golang.org/x/oauth2"
)

type googleUser struct {
	ID      string `json:"id"`
	Email   string `json:"email"`
	Name    string `json:"name"`
	Picture string `json:"picture"`
}

func (s *Service) getGoogleUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	data, err := fetchUserData[googleUser](ctx, token, "https://www.googleapis.com/oauth2/v2/userinfo", nil)
	if err != nil {
		return nil, err
	}

	return &UserInfo{
		ID:        data.ID,
		Email:     data.Email,
		Username:  data.Name,
		AvatarURL: data.Picture,
		Provider:  ProviderGoogle,
	}, nil
}
