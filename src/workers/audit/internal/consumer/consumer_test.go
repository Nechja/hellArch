package consumer

import (
	"encoding/json"
	"testing"
)

func TestRedactSensitiveFieldsRedactsPasswords(t *testing.T) {
	input := json.RawMessage(`{"username": "john", "password": "secret123"}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["username"] != "john" {
		t.Errorf("username should not be redacted, got: %v", m["username"])
	}
	if m["password"] != "[REDACTED]" {
		t.Errorf("password should be redacted, got: %v", m["password"])
	}
}

func TestRedactSensitiveFieldsRedactsTokens(t *testing.T) {
	input := json.RawMessage(`{"user_id": "123", "access_token": "eyJ...", "refresh_token": "abc..."}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["user_id"] != "123" {
		t.Errorf("user_id should not be redacted, got: %v", m["user_id"])
	}
	if m["access_token"] != "[REDACTED]" {
		t.Errorf("access_token should be redacted, got: %v", m["access_token"])
	}
	if m["refresh_token"] != "[REDACTED]" {
		t.Errorf("refresh_token should be redacted, got: %v", m["refresh_token"])
	}
}

func TestRedactSensitiveFieldsHandlesNestedObjects(t *testing.T) {
	input := json.RawMessage(`{
		"user": {
			"id": "123",
			"settings": {
				"password": "secret",
				"api_key": "key123"
			}
		}
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	user := m["user"].(map[string]any)
	if user["id"] != "123" {
		t.Errorf("user.id should not be redacted")
	}

	settings := user["settings"].(map[string]any)
	if settings["password"] != "[REDACTED]" {
		t.Errorf("nested password should be redacted, got: %v", settings["password"])
	}
	if settings["api_key"] != "[REDACTED]" {
		t.Errorf("nested api_key should be redacted, got: %v", settings["api_key"])
	}
}

func TestRedactSensitiveFieldsRedactsEntireCredentialsObject(t *testing.T) {
	// When a key name contains a sensitive word (like "credentials"),
	// the entire value should be redacted for security
	input := json.RawMessage(`{
		"user_credentials": {"password": "secret"},
		"auth_token_data": {"token": "abc123"}
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["user_credentials"] != "[REDACTED]" {
		t.Errorf("user_credentials should be entirely redacted, got: %v", m["user_credentials"])
	}
	if m["auth_token_data"] != "[REDACTED]" {
		t.Errorf("auth_token_data should be entirely redacted, got: %v", m["auth_token_data"])
	}
}

func TestRedactSensitiveFieldsHandlesArrays(t *testing.T) {
	input := json.RawMessage(`{
		"users": [
			{"name": "alice", "password": "pass1"},
			{"name": "bob", "password": "pass2"}
		]
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	users := m["users"].([]any)
	for i, u := range users {
		user := u.(map[string]any)
		if user["password"] != "[REDACTED]" {
			t.Errorf("users[%d].password should be redacted, got: %v", i, user["password"])
		}
	}
}

func TestRedactSensitiveFieldsPartialMatch(t *testing.T) {
	// Fields containing sensitive keywords should also be redacted
	input := json.RawMessage(`{
		"user_password": "secret",
		"auth_token": "abc",
		"database_credential": "pass"
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["user_password"] != "[REDACTED]" {
		t.Errorf("user_password should be redacted (partial match), got: %v", m["user_password"])
	}
	if m["auth_token"] != "[REDACTED]" {
		t.Errorf("auth_token should be redacted (partial match), got: %v", m["auth_token"])
	}
	if m["database_credential"] != "[REDACTED]" {
		t.Errorf("database_credential should be redacted (partial match), got: %v", m["database_credential"])
	}
}

func TestRedactSensitiveFieldsCaseInsensitive(t *testing.T) {
	input := json.RawMessage(`{
		"PASSWORD": "secret1",
		"Password": "secret2",
		"TOKEN": "abc"
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["PASSWORD"] != "[REDACTED]" {
		t.Errorf("PASSWORD (uppercase) should be redacted, got: %v", m["PASSWORD"])
	}
	if m["Password"] != "[REDACTED]" {
		t.Errorf("Password (mixed case) should be redacted, got: %v", m["Password"])
	}
	if m["TOKEN"] != "[REDACTED]" {
		t.Errorf("TOKEN (uppercase) should be redacted, got: %v", m["TOKEN"])
	}
}

func TestRedactSensitiveFieldsPreservesNonSensitiveData(t *testing.T) {
	input := json.RawMessage(`{
		"event_type": "user.created",
		"timestamp": "2024-01-01T00:00:00Z",
		"count": 42,
		"active": true
	}`)
	result := redactSensitiveFields(input)

	var m map[string]any
	if err := json.Unmarshal(result, &m); err != nil {
		t.Fatalf("failed to unmarshal result: %v", err)
	}

	if m["event_type"] != "user.created" {
		t.Errorf("event_type should be preserved, got: %v", m["event_type"])
	}
	if m["timestamp"] != "2024-01-01T00:00:00Z" {
		t.Errorf("timestamp should be preserved, got: %v", m["timestamp"])
	}
	if m["count"] != float64(42) { // JSON numbers are float64
		t.Errorf("count should be preserved, got: %v", m["count"])
	}
	if m["active"] != true {
		t.Errorf("active should be preserved, got: %v", m["active"])
	}
}

func TestRedactSensitiveFieldsHandlesInvalidJSON(t *testing.T) {
	input := json.RawMessage(`not valid json`)
	result := redactSensitiveFields(input)

	// Should return the original data unchanged
	if string(result) != string(input) {
		t.Errorf("invalid JSON should be returned unchanged")
	}
}

func TestIsSensitiveFieldExactMatch(t *testing.T) {
	sensitiveNames := []string{
		"password", "token", "secret", "api_key", "authorization",
	}

	for _, name := range sensitiveNames {
		if !isSensitiveField(name) {
			t.Errorf("%s should be identified as sensitive", name)
		}
	}
}

func TestIsSensitiveFieldPartialMatch(t *testing.T) {
	partialMatches := []string{
		"user_password", "auth_token", "api_secret", "my_api_key",
		"oauth_authorization", "db_credential",
	}

	for _, name := range partialMatches {
		if !isSensitiveField(name) {
			t.Errorf("%s should be identified as sensitive (partial match)", name)
		}
	}
}

func TestIsSensitiveFieldSafeNames(t *testing.T) {
	safeNames := []string{
		"username", "email", "created_at", "event_type", "user_id",
		"file_name", "count", "status",
	}

	for _, name := range safeNames {
		if isSensitiveField(name) {
			t.Errorf("%s should NOT be identified as sensitive", name)
		}
	}
}
