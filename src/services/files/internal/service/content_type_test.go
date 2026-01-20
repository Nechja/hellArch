package service

import (
	"errors"
	"testing"
)

func TestValidateContentType(t *testing.T) {
	t.Run("uses detected type when no claim", func(t *testing.T) {
		result, err := validateContentType("", "image/png")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/png" {
			t.Errorf("expected image/png, got %s", result)
		}
	})

	t.Run("uses detected type when claim is octet-stream", func(t *testing.T) {
		result, err := validateContentType("application/octet-stream", "image/jpeg")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/jpeg" {
			t.Errorf("expected image/jpeg, got %s", result)
		}
	})

	t.Run("trusts claimed type when detection fails", func(t *testing.T) {
		// When detected is octet-stream, we trust the claim for uncommon types
		result, err := validateContentType("application/x-custom", "application/octet-stream")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "application/x-custom" {
			t.Errorf("expected application/x-custom, got %s", result)
		}
	})
}

func TestValidateContentTypeSecurityBlocks(t *testing.T) {
	// These tests verify we block dangerous content type mismatches

	t.Run("blocks executable disguised as safe type", func(t *testing.T) {
		dangerousCases := []struct {
			claimed  string
			detected string
		}{
			{"image/png", "application/x-executable"},
			{"image/jpeg", "application/x-msdos-program"},
			{"text/plain", "application/x-msdownload"},
			{"application/pdf", "application/x-executable"},
		}

		for _, tc := range dangerousCases {
			_, err := validateContentType(tc.claimed, tc.detected)
			if !errors.Is(err, ErrContentTypeMismatch) {
				t.Errorf("claimed=%s detected=%s: expected ErrContentTypeMismatch, got %v",
					tc.claimed, tc.detected, err)
			}
		}
	})

	t.Run("blocks non-image disguised as image", func(t *testing.T) {
		// Claiming image/* but detected as non-image should fail
		cases := []struct {
			claimed  string
			detected string
		}{
			{"image/png", "text/html"},
			{"image/jpeg", "application/javascript"},
			{"image/gif", "text/plain"},
			{"image/webp", "application/xml"},
		}

		for _, tc := range cases {
			_, err := validateContentType(tc.claimed, tc.detected)
			if !errors.Is(err, ErrContentTypeMismatch) {
				t.Errorf("claimed=%s detected=%s: expected ErrContentTypeMismatch, got %v",
					tc.claimed, tc.detected, err)
			}
		}
	})

	t.Run("allows matching image types", func(t *testing.T) {
		// When both claimed and detected are images, should succeed
		result, err := validateContentType("image/png", "image/png")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/png" {
			t.Errorf("expected image/png, got %s", result)
		}
	})

	t.Run("allows different image subtypes", func(t *testing.T) {
		// Claimed png but detected jpeg - both are images, should use detected
		result, err := validateContentType("image/png", "image/jpeg")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/jpeg" {
			t.Errorf("expected image/jpeg (detected), got %s", result)
		}
	})
}

func TestValidateContentTypeNormalization(t *testing.T) {
	t.Run("strips charset parameter", func(t *testing.T) {
		// Content types often have parameters like charset
		result, err := validateContentType("text/plain; charset=utf-8", "text/plain; charset=us-ascii")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		// Should use the detected type
		if result != "text/plain; charset=us-ascii" {
			t.Errorf("expected detected type, got %s", result)
		}
	})

	t.Run("case insensitive comparison", func(t *testing.T) {
		// IMAGE/PNG should be treated same as image/png
		result, err := validateContentType("IMAGE/PNG", "image/png")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/png" {
			t.Errorf("expected image/png, got %s", result)
		}
	})

	t.Run("handles whitespace", func(t *testing.T) {
		result, err := validateContentType(" image/png ", "image/png")
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if result != "image/png" {
			t.Errorf("expected image/png, got %s", result)
		}
	})
}

func TestValidateContentTypeAllowsNonDangerous(t *testing.T) {
	// Non-image type mismatches that aren't security risks should be allowed
	cases := []struct {
		name     string
		claimed  string
		detected string
	}{
		{"text types can differ", "text/plain", "text/html"},
		{"json vs text", "application/json", "text/plain"},
		{"xml variations", "application/xml", "text/xml"},
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			_, err := validateContentType(tc.claimed, tc.detected)
			if err != nil {
				t.Errorf("expected no error for %s->%s, got %v", tc.claimed, tc.detected, err)
			}
		})
	}
}
