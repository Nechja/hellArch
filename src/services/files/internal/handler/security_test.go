package handler

import (
	"strings"
	"testing"
)

func TestSanitizeFilenameBlocksPathTraversal(t *testing.T) {
	maliciousInputs := []string{
		"../../../etc/passwd",
		"..\\..\\windows\\system32\\config\\sam",
		"/etc/shadow",
		"C:\\Windows\\System32\\config\\SAM",
		"....//....//etc/passwd",
		"..%2F..%2F..%2Fetc%2Fpasswd",
		"foo/../../../etc/passwd",
	}

	for _, input := range maliciousInputs {
		result := sanitizeFilename(input)

		// Result must not contain path separators or traversal sequences
		if strings.Contains(result, "/") {
			t.Errorf("sanitizeFilename(%q) = %q contains forward slash", input, result)
		}
		if strings.Contains(result, "\\") {
			t.Errorf("sanitizeFilename(%q) = %q contains backslash", input, result)
		}
		if strings.Contains(result, "..") {
			t.Errorf("sanitizeFilename(%q) = %q contains path traversal", input, result)
		}
	}
}

func TestSanitizeFilenameBlocksHeaderInjection(t *testing.T) {
	// These characters could be used for HTTP header injection
	injectionAttempts := []string{
		"file\r\nX-Injected: header",
		"file\nSet-Cookie: malicious=true",
		"file\rContent-Type: text/html",
		"file\"onload=\"alert(1)",
	}

	for _, input := range injectionAttempts {
		result := sanitizeFilename(input)

		if strings.ContainsAny(result, "\r\n\"") {
			t.Errorf("sanitizeFilename(%q) = %q contains injection characters", input, result)
		}
	}
}

func TestSanitizeFilenameHandlesEdgeCases(t *testing.T) {
	edgeCases := []struct {
		input       string
		mustNotBe   []string
		description string
	}{
		{
			input:       "",
			mustNotBe:   []string{"", "."},
			description: "empty string should become safe default",
		},
		{
			input:       ".",
			mustNotBe:   []string{"", "."},
			description: "single dot should become safe default",
		},
		{
			input:       "..",
			mustNotBe:   []string{"", ".", ".."},
			description: "double dot should become safe default",
		},
	}

	for _, tc := range edgeCases {
		t.Run(tc.description, func(t *testing.T) {
			result := sanitizeFilename(tc.input)
			for _, bad := range tc.mustNotBe {
				if result == bad {
					t.Errorf("sanitizeFilename(%q) = %q, should not be %q", tc.input, result, bad)
				}
			}
			if result == "" {
				t.Errorf("sanitizeFilename(%q) returned empty string", tc.input)
			}
		})
	}
}
