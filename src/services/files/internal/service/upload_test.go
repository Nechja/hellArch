package service

import (
	"bytes"
	"io"
	"testing"
)

type mockFile struct {
	*bytes.Reader
}

func (m *mockFile) Close() error { return nil }

func TestValidateContentTypeBlocksExecutablesDisguisedAsImages(t *testing.T) {
	// Attacker claims file is an image but it's actually an executable
	attacks := []struct {
		claimed  string
		detected string
	}{
		{"image/png", "application/x-executable"},
		{"image/jpeg", "application/x-msdownload"},
		{"image/gif", "application/x-msdos-program"},
		{"image/webp", "application/x-executable"},
	}

	for _, attack := range attacks {
		_, err := validateContentType(attack.claimed, attack.detected)
		if err == nil {
			t.Errorf("should block executable disguised as %s (detected: %s)", attack.claimed, attack.detected)
		}
	}
}

func TestValidateContentTypeBlocksNonImagesClaimedAsImages(t *testing.T) {
	// Attacker claims file is an image but detection says otherwise
	attacks := []struct {
		claimed  string
		detected string
	}{
		{"image/png", "text/html; charset=utf-8"},
		{"image/jpeg", "text/plain; charset=utf-8"},
		{"image/gif", "application/javascript"},
	}

	for _, attack := range attacks {
		_, err := validateContentType(attack.claimed, attack.detected)
		if err == nil {
			t.Errorf("should block non-image claimed as %s (detected: %s)", attack.claimed, attack.detected)
		}
	}
}

func TestDetectContentTypeResetsFilePosition(t *testing.T) {
	content := []byte("test content that should be fully readable after detection")
	file := &mockFile{Reader: bytes.NewReader(content)}

	_, err := detectContentType(file)
	if err != nil {
		t.Fatalf("detectContentType failed: %v", err)
	}

	// File must be readable from the beginning after detection
	// This catches bugs where we forget to seek back after reading magic bytes
	readBack, err := io.ReadAll(file)
	if err != nil {
		t.Fatalf("failed to read file after detection: %v", err)
	}

	if !bytes.Equal(readBack, content) {
		t.Errorf("file content corrupted after detection: got %d bytes, want %d bytes", len(readBack), len(content))
	}
}
