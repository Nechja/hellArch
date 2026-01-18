package jwt

import (
	"crypto/rand"
	"crypto/rsa"
	"testing"
	"time"
)

// generateTestKeys creates RSA keys for testing
func generateTestKeys(t *testing.T) (*rsa.PrivateKey, *rsa.PublicKey) {
	t.Helper()
	privateKey, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatalf("failed to generate RSA key: %v", err)
	}
	return privateKey, &privateKey.PublicKey
}

// TestGenerateJTI tests that JTI generation produces unique values
func TestGenerateJTI(t *testing.T) {
	seen := make(map[string]bool)
	iterations := 1000

	for i := 0; i < iterations; i++ {
		jti, err := generateJTI()
		if err != nil {
			t.Fatalf("generateJTI failed: %v", err)
		}

		// JTI should be non-empty
		if jti == "" {
			t.Error("generateJTI returned empty string")
		}

		// JTI should be unique
		if seen[jti] {
			t.Errorf("duplicate JTI generated: %s", jti)
		}
		seen[jti] = true

		// JTI should be URL-safe base64 (22 chars for 16 bytes without padding)
		if len(jti) != 22 {
			t.Errorf("JTI length = %d, want 22", len(jti))
		}
	}
}

// TestAccessTokenContainsJTI verifies that generated access tokens include JTI
func TestAccessTokenContainsJTI(t *testing.T) {
	privateKey, publicKey := generateTestKeys(t)
	service := &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}

	claims := &Claims{
		UserID:   "user-123",
		Email:    "test@example.com",
		Username: "testuser",
	}

	tokenString, err := service.GenerateAccessToken(claims, 15*time.Minute)
	if err != nil {
		t.Fatalf("GenerateAccessToken failed: %v", err)
	}

	// Parse and verify the token contains a JTI
	parsedClaims, err := service.ValidateToken(tokenString)
	if err != nil {
		t.Fatalf("ValidateToken failed: %v", err)
	}

	if parsedClaims.ID == "" {
		t.Error("access token should contain JTI (ID) claim")
	}
}

// TestRefreshTokenContainsJTI verifies that generated refresh tokens include JTI
func TestRefreshTokenContainsJTI(t *testing.T) {
	privateKey, publicKey := generateTestKeys(t)
	service := &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}

	tokenString, err := service.GenerateRefreshToken("user-123", 7*24*time.Hour)
	if err != nil {
		t.Fatalf("GenerateRefreshToken failed: %v", err)
	}

	// Parse and verify the token contains a JTI
	parsedClaims, err := service.ValidateToken(tokenString)
	if err != nil {
		t.Fatalf("ValidateToken failed: %v", err)
	}

	if parsedClaims.ID == "" {
		t.Error("refresh token should contain JTI (ID) claim")
	}
}

// TestTokensHaveUniqueJTI verifies that each generated token has a unique JTI
func TestTokensHaveUniqueJTI(t *testing.T) {
	privateKey, publicKey := generateTestKeys(t)
	service := &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}

	seen := make(map[string]bool)
	iterations := 100

	for i := 0; i < iterations; i++ {
		claims := &Claims{
			UserID:   "user-123",
			Email:    "test@example.com",
			Username: "testuser",
		}

		tokenString, err := service.GenerateAccessToken(claims, 15*time.Minute)
		if err != nil {
			t.Fatalf("GenerateAccessToken failed: %v", err)
		}

		parsedClaims, err := service.ValidateToken(tokenString)
		if err != nil {
			t.Fatalf("ValidateToken failed: %v", err)
		}

		if seen[parsedClaims.ID] {
			t.Errorf("duplicate JTI in tokens: %s", parsedClaims.ID)
		}
		seen[parsedClaims.ID] = true
	}
}

// TestTokenValidation tests basic token validation
func TestTokenValidation(t *testing.T) {
	privateKey, publicKey := generateTestKeys(t)
	service := &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}

	tests := []struct {
		name        string
		duration    time.Duration
		expectValid bool
	}{
		{"valid token", 15 * time.Minute, true},
		{"expired token", -15 * time.Minute, false},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			claims := &Claims{
				UserID:   "user-123",
				Email:    "test@example.com",
				Username: "testuser",
			}

			tokenString, err := service.GenerateAccessToken(claims, tt.duration)
			if err != nil {
				t.Fatalf("GenerateAccessToken failed: %v", err)
			}

			_, err = service.ValidateToken(tokenString)
			if tt.expectValid && err != nil {
				t.Errorf("expected valid token, got error: %v", err)
			}
			if !tt.expectValid && err == nil {
				t.Error("expected invalid token, got no error")
			}
		})
	}
}

// TestTokenSignatureValidation tests that tampering with token is detected
func TestTokenSignatureValidation(t *testing.T) {
	privateKey, publicKey := generateTestKeys(t)
	service := &Service{
		privateKey: privateKey,
		publicKey:  publicKey,
	}

	claims := &Claims{
		UserID:   "user-123",
		Email:    "test@example.com",
		Username: "testuser",
	}

	tokenString, err := service.GenerateAccessToken(claims, 15*time.Minute)
	if err != nil {
		t.Fatalf("GenerateAccessToken failed: %v", err)
	}

	// Tamper with the token
	tamperedToken := tokenString[:len(tokenString)-5] + "XXXXX"

	_, err = service.ValidateToken(tamperedToken)
	if err == nil {
		t.Error("expected error for tampered token, got nil")
	}
}
