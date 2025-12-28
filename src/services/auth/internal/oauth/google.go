package oauth

import (
	"context"
	"encoding/json"
	"io"
	"net/http"

	"golang.org/x/oauth2"
)

func (s *Service) getGoogleUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, "https://www.googleapis.com/oauth2/v2/userinfo", nil)
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
		ID      string `json:"id"`
		Email   string `json:"email"`
		Name    string `json:"name"`
		Picture string `json:"picture"`
	}
	if err := json.Unmarshal(body, &data); err != nil {
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
