package handler

import (
	"net"
	"strings"
	"testing"
	"time"

	"github.com/nechja/hellarch/services/events/internal/store"
)

func TestValidateWebhookURLBlocksSSRFTargets(t *testing.T) {
	ssrfTargets := []string{
		// Localhost variations
		"http://localhost/admin",
		"http://localhost:8080/internal",
		"http://127.0.0.1/secret",
		"http://127.0.0.1:3000/api",
		"http://[::1]/admin",

		// Private network ranges (RFC 1918)
		"http://10.0.0.1/internal",
		"http://10.255.255.255/api",
		"http://172.16.0.1/admin",
		"http://172.31.255.255/secret",
		"http://192.168.1.1/router",
		"http://192.168.0.100:8080/internal",

		// Cloud metadata endpoints
		"http://169.254.169.254/latest/meta-data/",
		"http://169.254.169.254/computeMetadata/v1/",

		// Invalid schemes
		"file:///etc/passwd",
		"ftp://internal-server/data",
		"gopher://localhost/",
	}

	for _, target := range ssrfTargets {
		err := validateWebhookURL(target)
		if err == nil {
			t.Errorf("should block SSRF target: %s", target)
		}
	}
}

func TestValidateWebhookURLAllowsPublicEndpoints(t *testing.T) {
	validURLs := []string{
		"https://hooks.slack.com/services/xxx",
		"https://discord.com/api/webhooks/xxx",
		"https://api.example.com/webhook",
		"http://webhook.site/abc123",
	}

	for _, url := range validURLs {
		err := validateWebhookURL(url)
		if err != nil {
			t.Errorf("should allow public endpoint %s: %v", url, err)
		}
	}
}

func TestIsPrivateIPBlocksAllPrivateRanges(t *testing.T) {
	privateIPs := []string{
		// Loopback
		"127.0.0.1",
		"127.255.255.255",

		// 10.0.0.0/8
		"10.0.0.0",
		"10.255.255.255",

		// 172.16.0.0/12
		"172.16.0.0",
		"172.31.255.255",

		// 192.168.0.0/16
		"192.168.0.0",
		"192.168.255.255",

		// Link-local / metadata
		"169.254.169.254",
		"169.254.0.1",
	}

	for _, ipStr := range privateIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse test IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should identify %s as private IP", ipStr)
		}
	}
}

func TestValidateEventTypesRejectsInvalidFormats(t *testing.T) {
	invalidTypes := []string{
		"",                    // empty
		"user",                // no dot
		"User.Created",        // uppercase
		"user.created.foo.1",  // starts with number after dot
		".user.created",       // starts with dot
		"user.created.",       // ends with dot
		"user..created",       // double dot
		"user-created",        // hyphen instead of dot
		"user_created",        // underscore instead of dot
		"user created",        // space
		"user.created!",       // special char
	}

	for _, eventType := range invalidTypes {
		err := validateEventTypes([]string{eventType})
		if err == nil {
			t.Errorf("should reject invalid event type: %q", eventType)
		}
	}
}

func TestValidateEventTypesAcceptsValidFormats(t *testing.T) {
	validTypes := []string{
		"user.created",
		"file.uploaded",
		"order.payment.completed",
		"system.health.check",
		"a.b",
		"auth.session.invalidated",
	}

	err := validateEventTypes(validTypes)
	if err != nil {
		t.Errorf("should accept valid event types: %v", err)
	}
}

func TestSanitizeWebhookRemovesSecret(t *testing.T) {
	webhook := &store.Webhook{
		ID:         "test-id",
		Name:       "Test Webhook",
		URL:        "https://example.com/hook",
		Secret:     "super-secret-key-12345",
		EventTypes: []string{"user.created"},
		Format:     store.WebhookFormatRaw,
		IsActive:   true,
		CreatedAt:  time.Now(),
		UpdatedAt:  time.Now(),
	}

	sanitized := sanitizeWebhook(webhook)

	// Secret must not appear anywhere in the sanitized response
	if strings.Contains(sanitized.URL, webhook.Secret) {
		t.Error("secret leaked in URL field")
	}

	// HasSecret should indicate a secret exists without revealing it
	if !sanitized.HasSecret {
		t.Error("HasSecret should be true when secret exists")
	}

	// Verify other fields are preserved
	if sanitized.ID != webhook.ID {
		t.Errorf("ID mismatch: got %s, want %s", sanitized.ID, webhook.ID)
	}
	if sanitized.Name != webhook.Name {
		t.Errorf("Name mismatch: got %s, want %s", sanitized.Name, webhook.Name)
	}
}

func TestSanitizeWebhooksDoesNotLeakSecrets(t *testing.T) {
	secrets := []string{"secret1", "secret2", "secret3"}
	webhooks := make([]*store.Webhook, len(secrets))

	for i, secret := range secrets {
		webhooks[i] = &store.Webhook{
			ID:         "id-" + secret,
			Secret:     secret,
			CreatedAt:  time.Now(),
			UpdatedAt:  time.Now(),
		}
	}

	sanitized := sanitizeWebhooks(webhooks)

	for i, s := range sanitized {
		for _, secret := range secrets {
			// Check that no secret appears in any field
			if s.ID == secret || s.Name == secret || s.URL == secret {
				t.Errorf("webhook %d leaks secret %q", i, secret)
			}
		}
	}
}
