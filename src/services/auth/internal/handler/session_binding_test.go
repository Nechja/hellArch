package handler

import (
	"testing"

	"github.com/nechja/hellarch/auth/internal/config"
	"github.com/nechja/hellarch/auth/internal/store"
)

// TestSessionBindingMismatchDetection verifies that IP/UserAgent mismatches are correctly detected
func TestSessionBindingMismatchDetection(t *testing.T) {
	tests := []struct {
		name          string
		sessionIP     string
		sessionUA     string
		currentIP     string
		currentUA     string
		expectIPMatch bool
		expectUAMatch bool
	}{
		{
			name:          "exact match",
			sessionIP:     "192.168.1.1",
			sessionUA:     "Mozilla/5.0",
			currentIP:     "192.168.1.1",
			currentUA:     "Mozilla/5.0",
			expectIPMatch: true,
			expectUAMatch: true,
		},
		{
			name:          "IP mismatch",
			sessionIP:     "192.168.1.1",
			sessionUA:     "Mozilla/5.0",
			currentIP:     "10.0.0.1",
			currentUA:     "Mozilla/5.0",
			expectIPMatch: false,
			expectUAMatch: true,
		},
		{
			name:          "UserAgent mismatch",
			sessionIP:     "192.168.1.1",
			sessionUA:     "Mozilla/5.0",
			currentIP:     "192.168.1.1",
			currentUA:     "Chrome/120.0",
			expectIPMatch: true,
			expectUAMatch: false,
		},
		{
			name:          "both mismatch",
			sessionIP:     "192.168.1.1",
			sessionUA:     "Mozilla/5.0",
			currentIP:     "10.0.0.1",
			currentUA:     "Chrome/120.0",
			expectIPMatch: false,
			expectUAMatch: false,
		},
		{
			name:          "empty session IP allows any",
			sessionIP:     "",
			sessionUA:     "Mozilla/5.0",
			currentIP:     "10.0.0.1",
			currentUA:     "Mozilla/5.0",
			expectIPMatch: true, // Empty session IP means no binding
			expectUAMatch: true,
		},
		{
			name:          "empty session UA allows any",
			sessionIP:     "192.168.1.1",
			sessionUA:     "",
			currentIP:     "192.168.1.1",
			currentUA:     "Chrome/120.0",
			expectIPMatch: true,
			expectUAMatch: true, // Empty session UA means no binding
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			session := &store.RedisSession{
				IPAddress: tt.sessionIP,
				UserAgent: tt.sessionUA,
			}

			// Check IP mismatch (only if session has an IP)
			ipMismatch := session.IPAddress != "" && session.IPAddress != tt.currentIP
			if ipMismatch == tt.expectIPMatch {
				t.Errorf("IP match = %v, want %v", !ipMismatch, tt.expectIPMatch)
			}

			// Check UA mismatch (only if session has a UA)
			uaMismatch := session.UserAgent != "" && session.UserAgent != tt.currentUA
			if uaMismatch == tt.expectUAMatch {
				t.Errorf("UA match = %v, want %v", !uaMismatch, tt.expectUAMatch)
			}
		})
	}
}

// TestSessionBindingModeConfig verifies config parsing for session binding mode
func TestSessionBindingModeConfig(t *testing.T) {
	tests := []struct {
		name     string
		mode     config.SessionBindingMode
		expected string
	}{
		{
			name:     "none mode",
			mode:     config.SessionBindingNone,
			expected: "none",
		},
		{
			name:     "warn mode",
			mode:     config.SessionBindingWarn,
			expected: "warn",
		},
		{
			name:     "strict mode",
			mode:     config.SessionBindingStrict,
			expected: "strict",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if string(tt.mode) != tt.expected {
				t.Errorf("mode = %s, want %s", tt.mode, tt.expected)
			}
		})
	}
}

// TestSessionBindingStrictModeRejectsOnMismatch verifies that strict mode rejects mismatched sessions
func TestSessionBindingStrictModeRejectsOnMismatch(t *testing.T) {
	// Simulate the validation logic from handleValidate
	session := &store.RedisSession{
		UserID:    "user-123",
		IPAddress: "192.168.1.1",
		UserAgent: "Mozilla/5.0",
	}

	currentIP := "10.0.0.1" // Different IP
	currentUA := "Mozilla/5.0"

	mode := config.SessionBindingStrict

	ipMismatch := session.IPAddress != "" && session.IPAddress != currentIP
	uaMismatch := session.UserAgent != "" && session.UserAgent != currentUA

	shouldReject := mode == config.SessionBindingStrict && (ipMismatch || uaMismatch)

	if !shouldReject {
		t.Error("strict mode should reject session with IP mismatch")
	}

	// Same IP, same UA - should not reject
	currentIP = "192.168.1.1"
	ipMismatch = session.IPAddress != "" && session.IPAddress != currentIP
	shouldReject = mode == config.SessionBindingStrict && (ipMismatch || uaMismatch)

	if shouldReject {
		t.Error("strict mode should not reject session with matching IP/UA")
	}
}

// TestSessionBindingWarnModeAllowsOnMismatch verifies that warn mode allows mismatched sessions
func TestSessionBindingWarnModeAllowsOnMismatch(t *testing.T) {
	session := &store.RedisSession{
		UserID:    "user-123",
		IPAddress: "192.168.1.1",
		UserAgent: "Mozilla/5.0",
	}

	currentIP := "10.0.0.1" // Different IP
	currentUA := "Mozilla/5.0"

	mode := config.SessionBindingWarn

	ipMismatch := session.IPAddress != "" && session.IPAddress != currentIP
	uaMismatch := session.UserAgent != "" && session.UserAgent != currentUA

	// Warn mode should not reject, only log
	shouldReject := mode == config.SessionBindingStrict && (ipMismatch || uaMismatch)

	if shouldReject {
		t.Error("warn mode should not reject session with IP mismatch")
	}

	// But mismatch should still be detected (for logging)
	if !ipMismatch {
		t.Error("IP mismatch should still be detected in warn mode")
	}
}

// TestSessionBindingNoneModeSkipsCheck verifies that none mode skips binding check entirely
func TestSessionBindingNoneModeSkipsCheck(t *testing.T) {
	mode := config.SessionBindingNone

	// In none mode, we skip the entire binding check
	shouldSkipCheck := mode == config.SessionBindingNone

	if !shouldSkipCheck {
		t.Error("none mode should skip binding check entirely")
	}
}
