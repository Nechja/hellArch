package jwt

import (
	"crypto/rand"
	"crypto/rsa"
	"encoding/base64"
	"fmt"
	"os"
	"time"

	"github.com/golang-jwt/jwt/v5"
)

// generateJTI creates a cryptographically secure unique token identifier
// for use in the JWT ID (jti) claim. This enables per-token revocation.
func generateJTI() (string, error) {
	b := make([]byte, 16)
	if _, err := rand.Read(b); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(b), nil
}

type Service struct {
	privateKey *rsa.PrivateKey
	publicKey  *rsa.PublicKey
}

type Claims struct {
	jwt.RegisteredClaims
	UserID   string            `json:"uid"`
	Email    string            `json:"email,omitempty"`
	Username string            `json:"username,omitempty"`
	Provider string            `json:"provider,omitempty"`
	Custom   map[string]string `json:"custom,omitempty"`
}

func NewService(privateKeyPath, publicKeyPath string) (*Service, error) {
	privateKeyBytes, err := os.ReadFile(privateKeyPath)
	if err != nil {
		return nil, fmt.Errorf("failed to read private key: %w", err)
	}

	privateKey, err := jwt.ParseRSAPrivateKeyFromPEM(privateKeyBytes)
	if err != nil {
		return nil, fmt.Errorf("failed to parse private key: %w", err)
	}

	publicKeyBytes, err := os.ReadFile(publicKeyPath)
	if err != nil {
		return nil, fmt.Errorf("failed to read public key: %w", err)
	}

	publicKey, err := jwt.ParseRSAPublicKeyFromPEM(publicKeyBytes)
	if err != nil {
		return nil, fmt.Errorf("failed to parse public key: %w", err)
	}

	return &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}, nil
}

func (s *Service) GenerateAccessToken(claims *Claims, duration time.Duration) (string, error) {
	jti, err := generateJTI()
	if err != nil {
		return "", fmt.Errorf("failed to generate jti: %w", err)
	}

	now := time.Now()
	claims.RegisteredClaims = jwt.RegisteredClaims{
		ID:        jti, // JTI claim for per-token revocation
		Issuer:    "hellarch-auth",
		Subject:   claims.UserID,
		IssuedAt:  jwt.NewNumericDate(now),
		ExpiresAt: jwt.NewNumericDate(now.Add(duration)),
		NotBefore: jwt.NewNumericDate(now),
	}

	token := jwt.NewWithClaims(jwt.SigningMethodRS256, claims)
	return token.SignedString(s.privateKey)
}

func (s *Service) GenerateRefreshToken(userID string, duration time.Duration) (string, error) {
	jti, err := generateJTI()
	if err != nil {
		return "", fmt.Errorf("failed to generate jti: %w", err)
	}

	now := time.Now()
	claims := &jwt.RegisteredClaims{
		ID:        jti, // JTI claim for per-token revocation
		Issuer:    "hellarch-auth",
		Subject:   userID,
		IssuedAt:  jwt.NewNumericDate(now),
		ExpiresAt: jwt.NewNumericDate(now.Add(duration)),
		NotBefore: jwt.NewNumericDate(now),
	}

	token := jwt.NewWithClaims(jwt.SigningMethodRS256, claims)
	return token.SignedString(s.privateKey)
}

func (s *Service) ValidateToken(tokenString string) (*Claims, error) {
	token, err := jwt.ParseWithClaims(tokenString, &Claims{}, func(token *jwt.Token) (interface{}, error) {
		if _, ok := token.Method.(*jwt.SigningMethodRSA); !ok {
			return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
		}
		return s.publicKey, nil
	})

	if err != nil {
		return nil, fmt.Errorf("failed to parse token: %w", err)
	}

	claims, ok := token.Claims.(*Claims)
	if !ok || !token.Valid {
		return nil, fmt.Errorf("invalid token")
	}

	return claims, nil
}

func (s *Service) GetPublicKey() *rsa.PublicKey {
	return s.publicKey
}
