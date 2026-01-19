package handler

import (
	"testing"
)

func TestEventTypePatternValidation(t *testing.T) {
	valid := []struct {
		name      string
		eventType string
	}{
		{"simple two parts", "user.created"},
		{"three parts", "user.profile.updated"},
		{"four parts", "system.health.check.failed"},
		{"with numbers", "v2.user.created"},
		{"numbers in middle", "user.v2.created"},
		{"all lowercase with numbers", "api1.user2.event3"},
	}

	for _, tt := range valid {
		t.Run("valid: "+tt.name, func(t *testing.T) {
			if !publishEventTypePattern.MatchString(tt.eventType) {
				t.Errorf("expected %q to be valid", tt.eventType)
			}
		})
	}

	invalid := []struct {
		name      string
		eventType string
	}{
		{"single word", "created"},
		{"uppercase", "User.Created"},
		{"mixed case", "user.Created"},
		{"starts with number", "1user.created"},
		{"underscore", "user_profile.created"},
		{"hyphen", "user-profile.created"},
		{"leading dot", ".user.created"},
		{"trailing dot", "user.created."},
		{"double dot", "user..created"},
		{"spaces", "user. created"},
		{"empty string", ""},
		{"just dots", "..."},
		{"special chars", "user@created.event"},
		{"part starts with number", "user.1created"},
	}

	for _, tt := range invalid {
		t.Run("invalid: "+tt.name, func(t *testing.T) {
			if publishEventTypePattern.MatchString(tt.eventType) {
				t.Errorf("expected %q to be invalid", tt.eventType)
			}
		})
	}
}

func TestEventTypePatternBoundaries(t *testing.T) {
	// These test edge cases that could break with regex changes

	t.Run("minimum valid event type", func(t *testing.T) {
		// Smallest possible valid event type: "a.b"
		if !publishEventTypePattern.MatchString("a.b") {
			t.Error("expected 'a.b' to be valid (minimum length)")
		}
	})

	t.Run("single char parts are valid", func(t *testing.T) {
		if !publishEventTypePattern.MatchString("a.b.c.d.e") {
			t.Error("expected single char parts to be valid")
		}
	})

	t.Run("long event type", func(t *testing.T) {
		longType := "service.domain.subdomain.action.detail.extra"
		if !publishEventTypePattern.MatchString(longType) {
			t.Errorf("expected long event type %q to be valid", longType)
		}
	})

	t.Run("number only part after letter start", func(t *testing.T) {
		// "a1.b2" - starts with letter, has numbers
		if !publishEventTypePattern.MatchString("a1.b2") {
			t.Error("expected 'a1.b2' to be valid")
		}
	})
}
