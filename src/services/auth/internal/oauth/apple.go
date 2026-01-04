package oauth

import (
	"context"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rsa"
	"crypto/x509"
	"encoding/base64"
	"encoding/json"
	"encoding/pem"
	"fmt"
	"math/big"
	"net/http"
	"os"
	"strings"
	"sync"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/nechja/hellarch/auth/internal/config"
	"golang.org/x/oauth2"
)

const appleJWKSURL = "https://appleid.apple.com/auth/keys"

type appleJWKS struct {
	Keys []appleJWK `json:"keys"`
}

type appleJWK struct {
	Kty string `json:"kty"`
	Kid string `json:"kid"`
	Use string `json:"use"`
	Alg string `json:"alg"`
	N   string `json:"n"`
	E   string `json:"e"`
	X   string `json:"x"`
	Y   string `json:"y"`
	Crv string `json:"crv"`
}

type appleIDTokenClaims struct {
	jwt.RegisteredClaims
	Email string `json:"email"`
}

var (
	appleJWKSCache     *appleJWKS
	appleJWKSCacheLock sync.RWMutex
	appleJWKSCacheTime time.Time
)

func (s *Service) getAppleUserInfo(ctx context.Context, token *oauth2.Token) (*UserInfo, error) {
	idToken := token.Extra("id_token")
	if idToken == nil {
		return nil, fmt.Errorf("no id_token in response")
	}

	idTokenStr, ok := idToken.(string)
	if !ok {
		return nil, fmt.Errorf("id_token is not a string")
	}

	cfg := s.configs[ProviderApple]
	if cfg == nil {
		return nil, fmt.Errorf("apple not configured")
	}

	claims, err := verifyAppleIDToken(ctx, idTokenStr, cfg.ClientID)
	if err != nil {
		return nil, fmt.Errorf("failed to verify id_token: %w", err)
	}

	username := claims.Email
	if atIdx := strings.Index(claims.Email, "@"); atIdx > 0 {
		username = claims.Email[:atIdx]
	}

	return &UserInfo{
		ID:        claims.Subject,
		Email:     claims.Email,
		Username:  username,
		AvatarURL: "",
		Provider:  ProviderApple,
	}, nil
}

func verifyAppleIDToken(ctx context.Context, idToken, clientID string) (*appleIDTokenClaims, error) {
	jwks, err := getAppleJWKS(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to fetch JWKS: %w", err)
	}

	token, err := jwt.ParseWithClaims(idToken, &appleIDTokenClaims{}, func(token *jwt.Token) (interface{}, error) {
		kid, ok := token.Header["kid"].(string)
		if !ok {
			return nil, fmt.Errorf("missing kid in token header")
		}

		key, err := findAppleKey(jwks, kid)
		if err != nil {
			return nil, err
		}
		return key, nil
	}, jwt.WithValidMethods([]string{"ES256", "RS256"}))

	if err != nil {
		return nil, fmt.Errorf("token validation failed: %w", err)
	}

	claims, ok := token.Claims.(*appleIDTokenClaims)
	if !ok || !token.Valid {
		return nil, fmt.Errorf("invalid token claims")
	}

	if claims.Issuer != "https://appleid.apple.com" {
		return nil, fmt.Errorf("invalid issuer: %s", claims.Issuer)
	}

	if !containsAudience(claims.Audience, clientID) {
		return nil, fmt.Errorf("invalid audience")
	}

	return claims, nil
}

func containsAudience(audiences jwt.ClaimStrings, clientID string) bool {
	for _, aud := range audiences {
		if aud == clientID {
			return true
		}
	}
	return false
}

func getAppleJWKS(ctx context.Context) (*appleJWKS, error) {
	appleJWKSCacheLock.RLock()
	if appleJWKSCache != nil && time.Since(appleJWKSCacheTime) < time.Hour {
		defer appleJWKSCacheLock.RUnlock()
		return appleJWKSCache, nil
	}
	appleJWKSCacheLock.RUnlock()

	appleJWKSCacheLock.Lock()
	defer appleJWKSCacheLock.Unlock()

	if appleJWKSCache != nil && time.Since(appleJWKSCacheTime) < time.Hour {
		return appleJWKSCache, nil
	}

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, appleJWKSURL, nil)
	if err != nil {
		return nil, err
	}

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("failed to fetch JWKS: status %d", resp.StatusCode)
	}

	var jwks appleJWKS
	if err := json.NewDecoder(resp.Body).Decode(&jwks); err != nil {
		return nil, err
	}

	appleJWKSCache = &jwks
	appleJWKSCacheTime = time.Now()
	return &jwks, nil
}

func findAppleKey(jwks *appleJWKS, kid string) (interface{}, error) {
	for _, key := range jwks.Keys {
		if key.Kid == kid {
			return parseJWK(key)
		}
	}
	return nil, fmt.Errorf("key %s not found in JWKS", kid)
}

func parseJWK(key appleJWK) (interface{}, error) {
	switch key.Kty {
	case "EC":
		return parseECKey(key)
	case "RSA":
		return parseRSAKey(key)
	default:
		return nil, fmt.Errorf("unsupported key type: %s", key.Kty)
	}
}

func parseECKey(key appleJWK) (*ecdsa.PublicKey, error) {
	xBytes, err := base64.RawURLEncoding.DecodeString(key.X)
	if err != nil {
		return nil, fmt.Errorf("failed to decode X: %w", err)
	}

	yBytes, err := base64.RawURLEncoding.DecodeString(key.Y)
	if err != nil {
		return nil, fmt.Errorf("failed to decode Y: %w", err)
	}

	var curve elliptic.Curve
	switch key.Crv {
	case "P-256":
		curve = elliptic.P256()
	case "P-384":
		curve = elliptic.P384()
	case "P-521":
		curve = elliptic.P521()
	default:
		return nil, fmt.Errorf("unsupported curve: %s", key.Crv)
	}

	return &ecdsa.PublicKey{
		Curve: curve,
		X:     new(big.Int).SetBytes(xBytes),
		Y:     new(big.Int).SetBytes(yBytes),
	}, nil
}

func parseRSAKey(key appleJWK) (*rsa.PublicKey, error) {
	nBytes, err := base64.RawURLEncoding.DecodeString(key.N)
	if err != nil {
		return nil, fmt.Errorf("failed to decode N: %w", err)
	}

	eBytes, err := base64.RawURLEncoding.DecodeString(key.E)
	if err != nil {
		return nil, fmt.Errorf("failed to decode E: %w", err)
	}

	return &rsa.PublicKey{
		N: new(big.Int).SetBytes(nBytes),
		E: int(new(big.Int).SetBytes(eBytes).Int64()),
	}, nil
}

func generateAppleClientSecret(cfg *config.AppleConfig) (string, error) {
	keyBytes, err := os.ReadFile(cfg.PrivateKeyPath)
	if err != nil {
		return "", fmt.Errorf("failed to read private key: %w", err)
	}

	block, _ := pem.Decode(keyBytes)
	if block == nil {
		return "", fmt.Errorf("failed to decode PEM block")
	}

	privateKey, err := x509.ParsePKCS8PrivateKey(block.Bytes)
	if err != nil {
		return "", fmt.Errorf("failed to parse private key: %w", err)
	}

	ecKey, ok := privateKey.(*ecdsa.PrivateKey)
	if !ok {
		return "", fmt.Errorf("private key is not ECDSA")
	}

	now := time.Now()
	claims := jwt.RegisteredClaims{
		Issuer:    cfg.TeamID,
		Subject:   cfg.ClientID,
		Audience:  jwt.ClaimStrings{"https://appleid.apple.com"},
		IssuedAt:  jwt.NewNumericDate(now),
		ExpiresAt: jwt.NewNumericDate(now.Add(time.Hour)),
	}

	token := jwt.NewWithClaims(jwt.SigningMethodES256, claims)
	token.Header["kid"] = cfg.KeyID

	return token.SignedString(ecKey)
}
