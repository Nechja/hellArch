package dispatcher

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"strings"
	"testing"
)

func TestComputeSignature(t *testing.T) {
	t.Run("produces sha256 prefixed signature", func(t *testing.T) {
		payload := []byte(`{"test": "data"}`)
		secret := "test-secret"

		result := computeSignature(payload, secret)

		if !strings.HasPrefix(result, "sha256=") {
			t.Errorf("expected sha256= prefix, got %q", result)
		}
	})

	t.Run("produces valid HMAC-SHA256", func(t *testing.T) {
		payload := []byte(`{"test": "data"}`)
		secret := "test-secret"

		result := computeSignature(payload, secret)

		// Verify manually
		mac := hmac.New(sha256.New, []byte(secret))
		mac.Write(payload)
		expected := "sha256=" + hex.EncodeToString(mac.Sum(nil))

		if result != expected {
			t.Errorf("signature mismatch:\ngot:  %s\nwant: %s", result, expected)
		}
	})

	t.Run("different payloads produce different signatures", func(t *testing.T) {
		secret := "same-secret"
		sig1 := computeSignature([]byte(`{"a": 1}`), secret)
		sig2 := computeSignature([]byte(`{"b": 2}`), secret)

		if sig1 == sig2 {
			t.Error("expected different signatures for different payloads")
		}
	})

	t.Run("different secrets produce different signatures", func(t *testing.T) {
		payload := []byte(`{"test": "data"}`)
		sig1 := computeSignature(payload, "secret-1")
		sig2 := computeSignature(payload, "secret-2")

		if sig1 == sig2 {
			t.Error("expected different signatures for different secrets")
		}
	})

	t.Run("same payload and secret produce same signature", func(t *testing.T) {
		payload := []byte(`{"test": "data"}`)
		secret := "consistent-secret"

		sig1 := computeSignature(payload, secret)
		sig2 := computeSignature(payload, secret)

		if sig1 != sig2 {
			t.Error("expected same signature for same inputs")
		}
	})

	t.Run("empty payload produces valid signature", func(t *testing.T) {
		result := computeSignature([]byte{}, "secret")

		if !strings.HasPrefix(result, "sha256=") {
			t.Errorf("expected sha256= prefix for empty payload, got %q", result)
		}
		// Should have 64 hex chars after prefix
		hexPart := strings.TrimPrefix(result, "sha256=")
		if len(hexPart) != 64 {
			t.Errorf("expected 64 hex chars, got %d", len(hexPart))
		}
	})

	t.Run("empty secret produces valid signature", func(t *testing.T) {
		result := computeSignature([]byte(`{"test": "data"}`), "")

		if !strings.HasPrefix(result, "sha256=") {
			t.Errorf("expected sha256= prefix for empty secret, got %q", result)
		}
	})

	t.Run("signature is lowercase hex", func(t *testing.T) {
		result := computeSignature([]byte(`test`), "secret")
		hexPart := strings.TrimPrefix(result, "sha256=")

		for _, c := range hexPart {
			if !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')) {
				t.Errorf("expected lowercase hex, found char %q", string(c))
			}
		}
	})

	t.Run("can verify signature with standard library", func(t *testing.T) {
		payload := []byte(`{"webhook": "test"}`)
		secret := "webhook-secret-key"

		signature := computeSignature(payload, secret)
		hexSig := strings.TrimPrefix(signature, "sha256=")

		// Verify using standard approach
		mac := hmac.New(sha256.New, []byte(secret))
		mac.Write(payload)
		expectedMAC := mac.Sum(nil)

		actualMAC, err := hex.DecodeString(hexSig)
		if err != nil {
			t.Fatalf("failed to decode hex: %v", err)
		}

		if !hmac.Equal(actualMAC, expectedMAC) {
			t.Error("signature verification failed")
		}
	})
}

func TestSignatureVerification(t *testing.T) {
	// Test that signatures can be used for webhook verification
	t.Run("webhook receiver can verify signature", func(t *testing.T) {
		payload := []byte(`{"event": "user.created", "user_id": "123"}`)
		secret := "shared-webhook-secret"

		// Sender computes signature
		signature := computeSignature(payload, secret)

		// Receiver verifies (simulating webhook receiver)
		hexSig := strings.TrimPrefix(signature, "sha256=")
		receivedMAC, _ := hex.DecodeString(hexSig)

		mac := hmac.New(sha256.New, []byte(secret))
		mac.Write(payload)
		expectedMAC := mac.Sum(nil)

		if !hmac.Equal(receivedMAC, expectedMAC) {
			t.Error("receiver should be able to verify signature")
		}
	})

	t.Run("tampered payload fails verification", func(t *testing.T) {
		originalPayload := []byte(`{"amount": 100}`)
		tamperedPayload := []byte(`{"amount": 999}`)
		secret := "secure-secret"

		// Signature computed with original
		signature := computeSignature(originalPayload, secret)

		// Verify with tampered payload (should fail)
		hexSig := strings.TrimPrefix(signature, "sha256=")
		receivedMAC, _ := hex.DecodeString(hexSig)

		mac := hmac.New(sha256.New, []byte(secret))
		mac.Write(tamperedPayload)
		expectedMAC := mac.Sum(nil)

		if hmac.Equal(receivedMAC, expectedMAC) {
			t.Error("tampered payload should fail verification")
		}
	})

	t.Run("wrong secret fails verification", func(t *testing.T) {
		payload := []byte(`{"test": "data"}`)
		senderSecret := "sender-secret"
		wrongSecret := "wrong-secret"

		signature := computeSignature(payload, senderSecret)

		// Try to verify with wrong secret
		hexSig := strings.TrimPrefix(signature, "sha256=")
		receivedMAC, _ := hex.DecodeString(hexSig)

		mac := hmac.New(sha256.New, []byte(wrongSecret))
		mac.Write(payload)
		expectedMAC := mac.Sum(nil)

		if hmac.Equal(receivedMAC, expectedMAC) {
			t.Error("wrong secret should fail verification")
		}
	})
}
