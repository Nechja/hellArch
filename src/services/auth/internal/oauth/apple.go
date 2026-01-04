package oauth

import (
	"context"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"strings"

	"golang.org/x/oauth2"
)

type appleIDTokenClaims struct {
	Sub   string `json:"sub"`
	Email string `json:"email"`
}

func (s *Service) getAppleUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	idToken := token.Extra("id_token")
	if idToken == nil {
		return nil, fmt.Errorf("no id_token in response")
	}

	idTokenStr, ok := idToken.(string)
	if !ok {
		return nil, fmt.Errorf("id_token is not a string")
	}

	claims, err := parseAppleIDToken(idTokenStr)
	if err != nil {
		return nil, fmt.Errorf("failed to parse id_token: %w", err)
	}

	username := claims.Email
	if atIdx := strings.Index(claims.Email, "@"); atIdx > 0 {
		username = claims.Email[:atIdx]
	}

	return &UserInfo{
		ID:        claims.Sub,
		Email:     claims.Email,
		Username:  username,
		AvatarURL: "",
		Provider:  ProviderApple,
	}, nil
}

func parseAppleIDToken(idToken string) (*appleIDTokenClaims, error) {
	parts := strings.Split(idToken, ".")
	if len(parts) != 3 {
		return nil, fmt.Errorf("invalid id_token format")
	}

	payload, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return nil, fmt.Errorf("failed to decode payload: %w", err)
	}

	var claims appleIDTokenClaims
	if err := json.Unmarshal(payload, &claims); err != nil {
		return nil, fmt.Errorf("failed to unmarshal claims: %w", err)
	}

	return &claims, nil
}
