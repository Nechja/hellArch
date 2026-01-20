package dispatcher

import (
	"encoding/json"
	"strings"
	"testing"

	"github.com/nechja/hellarch/golibs/events"
)

func TestFormatEventTitle(t *testing.T) {
	tests := []struct {
		name     string
		input    string
		expected string
	}{
		{
			name:     "standard event type",
			input:    "hellarch.auth.user.created.v1",
			expected: "User Created",
		},
		{
			name:     "session event",
			input:    "hellarch.auth.session.deleted.v1",
			expected: "Session Deleted",
		},
		{
			name:     "file event",
			input:    "hellarch.files.file.uploaded.v1",
			expected: "File Uploaded",
		},
		{
			name:     "short event type returns as-is",
			input:    "short.event",
			expected: "short.event",
		},
		{
			name:     "three part event returns as-is",
			input:    "one.two.three",
			expected: "one.two.three",
		},
		{
			name:     "single word returns as-is",
			input:    "event",
			expected: "event",
		},
		{
			name:     "empty string",
			input:    "",
			expected: "",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := formatEventTitle(tt.input)
			if result != tt.expected {
				t.Errorf("formatEventTitle(%q) = %q, want %q", tt.input, result, tt.expected)
			}
		})
	}
}

func TestGetColorForEventType(t *testing.T) {
	tests := []struct {
		name          string
		eventType     string
		expectedColor int
		colorName     string
	}{
		// Success events (green)
		{"created event is green", "user.created", 0x2ECC71, "green"},
		{"completed event is green", "task.completed", 0x2ECC71, "green"},
		{"recovered event is green", "service.recovered", 0x2ECC71, "green"},
		{"unlocked event is green", "account.unlocked", 0x2ECC71, "green"},
		{"linked event is green", "account.linked", 0x2ECC71, "green"},

		// Error events (red)
		{"deleted event is red", "user.deleted", 0xE74C3C, "red"},
		{"failed event is red", "task.failed", 0xE74C3C, "red"},
		{"unhealthy event is red", "service.unhealthy", 0xE74C3C, "red"},
		{"locked event is red", "account.locked", 0xE74C3C, "red"},
		{"suspicious event is red", "login.suspicious", 0xE74C3C, "red"},
		{"revoked event is red", "token.revoked", 0xE74C3C, "red"},

		// Warning events (orange)
		{"expiring event is orange", "cert.expiring", 0xE67E22, "orange"},
		{"threshold event is orange", "quota.threshold", 0xE67E22, "orange"},
		{"exceeded event is orange", "rate.exceeded", 0xE67E22, "orange"},
		{"failing event is orange", "health.failing", 0xE67E22, "orange"},
		{"rolledback event is orange", "deploy.rolledback", 0xE67E22, "orange"},

		// Info events (blue)
		{"updated event is blue", "user.updated", 0x3498DB, "blue"},
		{"changed event is blue", "config.changed", 0x3498DB, "blue"},
		{"started event is blue", "job.started", 0x3498DB, "blue"},
		{"ended event is blue", "session.ended", 0x3498DB, "blue"},

		// Default (purple)
		{"unknown event is purple", "some.random.event", 0x9B59B6, "purple"},
		{"empty string is purple", "", 0x9B59B6, "purple"},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := getColorForEventType(tt.eventType)
			if result != tt.expectedColor {
				t.Errorf("getColorForEventType(%q) = 0x%X, want 0x%X (%s)",
					tt.eventType, result, tt.expectedColor, tt.colorName)
			}
		})
	}
}

func TestFormatFieldName(t *testing.T) {
	tests := []struct {
		name     string
		input    string
		expected string
	}{
		{"snake_case", "user_id", "User Id"},
		{"single word", "email", "Email"},
		{"multiple underscores", "first_name_field", "First Name Field"},
		{"already capitalized", "UserID", "Userid"},
		{"empty string", "", ""},
		{"leading underscore", "_private", " Private"},
		{"trailing underscore", "field_", "Field "},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := formatFieldName(tt.input)
			if result != tt.expected {
				t.Errorf("formatFieldName(%q) = %q, want %q", tt.input, result, tt.expected)
			}
		})
	}
}

func TestFormatValue(t *testing.T) {
	tests := []struct {
		name     string
		input    any
		expected string
	}{
		{"string value", "hello", "hello"},
		{"empty string", "", ""},
		{"integer as float64", float64(42), "42"},
		{"float value", float64(3.14159), "3.14"},
		{"negative integer", float64(-10), "-10"},
		{"zero", float64(0), "0"},
		{"true boolean", true, "true"},
		{"false boolean", false, "false"},
		{"nil value", nil, ""},
		{"map value", map[string]any{"key": "value"}, `{"key":"value"}`},
		{"array value", []any{"a", "b"}, `["a","b"]`},
		{"nested map", map[string]any{"nested": map[string]any{"deep": "value"}}, `{"nested":{"deep":"value"}}`},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := formatValue(tt.input)
			if result != tt.expected {
				t.Errorf("formatValue(%v) = %q, want %q", tt.input, result, tt.expected)
			}
		})
	}
}

func TestTruncate(t *testing.T) {
	tests := []struct {
		name     string
		input    string
		maxLen   int
		expected string
	}{
		{"short string unchanged", "hello", 10, "hello"},
		{"exact length unchanged", "hello", 5, "hello"},
		{"truncated with ellipsis", "hello world", 8, "hello..."},
		{"empty string", "", 10, ""},
		{"single char truncation", "abcdefghij", 5, "ab..."},
		{"maxLen less than 4", "hello", 3, "..."},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := truncate(tt.input, tt.maxLen)
			if result != tt.expected {
				t.Errorf("truncate(%q, %d) = %q, want %q", tt.input, tt.maxLen, result, tt.expected)
			}
		})
	}
}

func TestExtractFields(t *testing.T) {
	t.Run("empty data returns nil", func(t *testing.T) {
		result := extractFields(nil)
		if result != nil {
			t.Errorf("extractFields(nil) = %v, want nil", result)
		}
	})

	t.Run("invalid JSON returns nil", func(t *testing.T) {
		result := extractFields([]byte("not json"))
		if result != nil {
			t.Errorf("extractFields(invalid) = %v, want nil", result)
		}
	})

	t.Run("extracts simple fields", func(t *testing.T) {
		data := []byte(`{"user_id": "123", "email": "test@example.com"}`)
		result := extractFields(data)
		if result == nil {
			t.Fatal("expected fields, got nil")
		}
		if len(result) != 2 {
			t.Errorf("expected 2 fields, got %d", len(result))
		}
	})

	t.Run("respects Discord 25 field limit", func(t *testing.T) {
		// Create data with 30 fields
		dataMap := make(map[string]any)
		for i := 0; i < 30; i++ {
			dataMap[string(rune('a'+i))] = "value"
		}
		data, _ := json.Marshal(dataMap)

		result := extractFields(data)
		if len(result) > 25 {
			t.Errorf("expected max 25 fields, got %d", len(result))
		}
	})

	t.Run("truncates long field values", func(t *testing.T) {
		longValue := strings.Repeat("a", 2000)
		data, _ := json.Marshal(map[string]any{"field": longValue})

		result := extractFields(data)
		if result == nil || len(result) == 0 {
			t.Fatal("expected fields, got nil or empty")
		}
		if len(result[0].Value) > 1024 {
			t.Errorf("expected value truncated to 1024, got %d", len(result[0].Value))
		}
	})

	t.Run("sets inline for short values", func(t *testing.T) {
		data := []byte(`{"short": "hi", "long": "this is a much longer value that should not be inline because it exceeds fifty characters easily"}`)
		result := extractFields(data)

		var shortField, longField *DiscordEmbedField
		for i := range result {
			if result[i].Name == "Short" {
				shortField = &result[i]
			}
			if result[i].Name == "Long" {
				longField = &result[i]
			}
		}

		if shortField != nil && !shortField.Inline {
			t.Error("expected short field to be inline")
		}
		if longField != nil && longField.Inline {
			t.Error("expected long field to not be inline")
		}
	})
}

func TestExtractSlackFields(t *testing.T) {
	t.Run("empty data returns nil", func(t *testing.T) {
		result := extractSlackFields(nil)
		if result != nil {
			t.Errorf("extractSlackFields(nil) = %v, want nil", result)
		}
	})

	t.Run("respects Slack 10 field limit", func(t *testing.T) {
		dataMap := make(map[string]any)
		for i := 0; i < 15; i++ {
			dataMap[string(rune('a'+i))] = "value"
		}
		data, _ := json.Marshal(dataMap)

		result := extractSlackFields(data)
		if len(result) > 10 {
			t.Errorf("expected max 10 fields, got %d", len(result))
		}
	})

	t.Run("formats as mrkdwn", func(t *testing.T) {
		data := []byte(`{"user_id": "123"}`)
		result := extractSlackFields(data)

		if result == nil || len(result) == 0 {
			t.Fatal("expected fields")
		}
		if result[0].Type != "mrkdwn" {
			t.Errorf("expected type mrkdwn, got %s", result[0].Type)
		}
	})
}

func TestTransformPayload(t *testing.T) {
	validCloudEvent := []byte(`{
		"specversion": "1.0",
		"type": "hellarch.test.event.created.v1",
		"source": "test-service",
		"id": "test-123",
		"time": "2024-01-01T00:00:00Z",
		"data": {"user_id": "456"}
	}`)

	t.Run("unknown format returns original payload", func(t *testing.T) {
		result, contentType, err := TransformPayload(validCloudEvent, events.WebhookFormat("unknown"))
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if contentType != "application/cloudevents+json" {
			t.Errorf("expected cloudevents content type, got %s", contentType)
		}
		if string(result) != string(validCloudEvent) {
			t.Error("expected original payload returned")
		}
	})

	t.Run("raw format returns original payload", func(t *testing.T) {
		result, contentType, err := TransformPayload(validCloudEvent, events.WebhookFormatRaw)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if contentType != "application/cloudevents+json" {
			t.Errorf("expected cloudevents content type, got %s", contentType)
		}
		if string(result) != string(validCloudEvent) {
			t.Error("expected original payload returned")
		}
	})

	t.Run("Discord format transforms payload", func(t *testing.T) {
		result, contentType, err := TransformPayload(validCloudEvent, events.WebhookFormatDiscord)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if contentType != "application/json" {
			t.Errorf("expected application/json, got %s", contentType)
		}

		var discord DiscordPayload
		if err := json.Unmarshal(result, &discord); err != nil {
			t.Fatalf("failed to parse Discord payload: %v", err)
		}
		if len(discord.Embeds) != 1 {
			t.Errorf("expected 1 embed, got %d", len(discord.Embeds))
		}
		if discord.Embeds[0].Title != "Event Created" {
			t.Errorf("expected title 'Event Created', got %q", discord.Embeds[0].Title)
		}
	})

	t.Run("Slack format transforms payload", func(t *testing.T) {
		result, contentType, err := TransformPayload(validCloudEvent, events.WebhookFormatSlack)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if contentType != "application/json" {
			t.Errorf("expected application/json, got %s", contentType)
		}

		var slack SlackPayload
		if err := json.Unmarshal(result, &slack); err != nil {
			t.Fatalf("failed to parse Slack payload: %v", err)
		}
		if slack.Text != "Event Created" {
			t.Errorf("expected text 'Event Created', got %q", slack.Text)
		}
	})

	t.Run("Discord transform fails on invalid JSON", func(t *testing.T) {
		_, _, err := TransformPayload([]byte("not json"), events.WebhookFormatDiscord)
		if err == nil {
			t.Error("expected error for invalid JSON")
		}
	})

	t.Run("Slack transform fails on invalid JSON", func(t *testing.T) {
		_, _, err := TransformPayload([]byte("not json"), events.WebhookFormatSlack)
		if err == nil {
			t.Error("expected error for invalid JSON")
		}
	})
}

func TestTransformToDiscord(t *testing.T) {
	t.Run("includes subject in description", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "test.event.v1",
			"source": "test",
			"id": "123",
			"subject": "user-456"
		}`)

		result, _, err := transformToDiscord(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var discord DiscordPayload
		json.Unmarshal(result, &discord)

		if !strings.Contains(discord.Embeds[0].Description, "user-456") {
			t.Error("expected subject in description")
		}
	})

	t.Run("sets footer with source", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "test.event.v1",
			"source": "auth-service",
			"id": "123"
		}`)

		result, _, err := transformToDiscord(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var discord DiscordPayload
		json.Unmarshal(result, &discord)

		if discord.Embeds[0].Footer == nil {
			t.Fatal("expected footer")
		}
		if !strings.Contains(discord.Embeds[0].Footer.Text, "auth-service") {
			t.Error("expected source in footer")
		}
	})

	t.Run("preserves timestamp", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "test.event.v1",
			"source": "test",
			"id": "123",
			"time": "2024-01-15T10:30:00Z"
		}`)

		result, _, err := transformToDiscord(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var discord DiscordPayload
		json.Unmarshal(result, &discord)

		if discord.Embeds[0].Timestamp != "2024-01-15T10:30:00Z" {
			t.Errorf("expected timestamp preserved, got %q", discord.Embeds[0].Timestamp)
		}
	})
}

func TestTransformToSlack(t *testing.T) {
	t.Run("creates header block", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "hellarch.test.user.created.v1",
			"source": "test",
			"id": "123"
		}`)

		result, _, err := transformToSlack(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var slack SlackPayload
		json.Unmarshal(result, &slack)

		if len(slack.Blocks) == 0 {
			t.Fatal("expected blocks")
		}
		if slack.Blocks[0].Type != "header" {
			t.Errorf("expected header block first, got %s", slack.Blocks[0].Type)
		}
	})

	t.Run("includes subject in info section", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "test.event.v1",
			"source": "test",
			"id": "123",
			"subject": "important-subject"
		}`)

		result, _, err := transformToSlack(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var slack SlackPayload
		json.Unmarshal(result, &slack)

		found := false
		for _, block := range slack.Blocks {
			if block.Text != nil && strings.Contains(block.Text.Text, "important-subject") {
				found = true
				break
			}
		}
		if !found {
			t.Error("expected subject in info section")
		}
	})

	t.Run("adds context block with timestamp", func(t *testing.T) {
		event := []byte(`{
			"specversion": "1.0",
			"type": "test.event.v1",
			"source": "test",
			"id": "123",
			"time": "2024-01-15T10:30:00Z"
		}`)

		result, _, err := transformToSlack(event)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		var slack SlackPayload
		json.Unmarshal(result, &slack)

		hasContext := false
		for _, block := range slack.Blocks {
			if block.Type == "context" {
				hasContext = true
				break
			}
		}
		if !hasContext {
			t.Error("expected context block with timestamp")
		}
	})
}
