package oauth

import (
	"context"

	"golang.org/x/oauth2"
)

type hellarchUser struct {
	Sub      string `json:"sub"`
	Email    string `json:"email"`
	Username string `json:"username"`
}

func (s *Service) getHellArchUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	userInfoURL := s.hellarchBaseURL + "/identity/userinfo"

	data, err := fetchUserData[hellarchUser](ctx, s.httpClient, token, userInfoURL, nil)
	if err != nil {
		return nil, err
	}

	return &UserInfo{
		ID:        data.Sub,
		Email:     data.Email,
		Username:  data.Username,
		AvatarURL: "",
		Provider:  ProviderHellArch,
	}, nil
}
