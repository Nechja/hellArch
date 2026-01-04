package oauth

import (
	"context"

	"golang.org/x/oauth2"
)

type microsoftUser struct {
	ID                string `json:"id"`
	Mail              string `json:"mail"`
	UserPrincipalName string `json:"userPrincipalName"`
	DisplayName       string `json:"displayName"`
}

func (s *Service) getMicrosoftUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	data, err := fetchUserData[microsoftUser](ctx, token, "https://graph.microsoft.com/v1.0/me", nil)
	if err != nil {
		return nil, err
	}

	email := data.Mail
	if email == "" {
		email = data.UserPrincipalName
	}

	return &UserInfo{
		ID:        data.ID,
		Email:     email,
		Username:  data.DisplayName,
		AvatarURL: "",
		Provider:  ProviderMicrosoft,
	}, nil
}
