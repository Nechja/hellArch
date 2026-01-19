package handler

import (
	"testing"
)

// TestCSRFTokenValidation tests the CSRF token hashing and validation
func TestCSRFTokenValidation(t *testing.T) {
	tests := []struct {
		name      string
		token     string
		hash      string
		expectErr bool
	}{
		{
			name:      "valid token",
			token:     "test-csrf-token",
			expectErr: false,
		},
		{
			name:      "empty token",
			token:     "",
			expectErr: true,
		},
	}

	handler := &SecurityHandler{}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			// Generate hash for the token
			hash := handler.HashCSRFToken(tt.token)

			// Validate with correct token
			err := handler.ValidateCSRFToken(tt.token, hash)
			if tt.expectErr && err == nil {
				t.Error("expected error but got none")
			}
			if !tt.expectErr && err != nil {
				t.Errorf("unexpected error: %v", err)
			}

			// Validate with wrong token should fail
			if !tt.expectErr {
				err = handler.ValidateCSRFToken("wrong-token", hash)
				if err == nil {
					t.Error("expected error for wrong token but got none")
				}
			}
		})
	}
}
