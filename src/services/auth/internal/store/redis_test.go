package store

import (
	"context"
	"sync"
	"testing"
	"time"

	"github.com/alicebob/miniredis/v2"
	"github.com/redis/go-redis/v9"
)

// setupTestRedis creates a miniredis instance for testing
func setupTestRedis(t *testing.T) (*Redis, func()) {
	t.Helper()

	mr, err := miniredis.Run()
	if err != nil {
		t.Fatalf("failed to start miniredis: %v", err)
	}

	client := redis.NewClient(&redis.Options{
		Addr: mr.Addr(),
	})

	r := &Redis{client: client}

	cleanup := func() {
		client.Close()
		mr.Close()
	}

	return r, cleanup
}

// TestGetAndDeleteOAuthStateAtomic tests that OAuth state retrieval and deletion
// is atomic to prevent race conditions
func TestGetAndDeleteOAuthStateAtomic(t *testing.T) {
	r, cleanup := setupTestRedis(t)
	defer cleanup()

	ctx := context.Background()
	state := "test-oauth-state"
	data := `{"redirect_uri":"/app","csrf_hash":"abc123"}`

	// Set state
	err := r.SetOAuthState(ctx, state, data, 5*time.Minute)
	if err != nil {
		t.Fatalf("failed to set oauth state: %v", err)
	}

	// Get and delete atomically
	result, err := r.GetAndDeleteOAuthState(ctx, state)
	if err != nil {
		t.Fatalf("failed to get and delete oauth state: %v", err)
	}
	if result != data {
		t.Errorf("got %q, want %q", result, data)
	}

	// Second call should return nil (state was deleted)
	result, err = r.GetAndDeleteOAuthState(ctx, state)
	if err != redis.Nil {
		t.Errorf("expected redis.Nil error, got %v", err)
	}
	if result != "" {
		t.Errorf("expected empty result after deletion, got %q", result)
	}
}

// TestGetAndDeleteOAuthStateConcurrent tests that concurrent calls to
// GetAndDeleteOAuthState only succeed once (atomicity)
func TestGetAndDeleteOAuthStateConcurrent(t *testing.T) {
	r, cleanup := setupTestRedis(t)
	defer cleanup()

	ctx := context.Background()
	state := "concurrent-state"
	data := `{"test":"data"}`

	// Set state
	err := r.SetOAuthState(ctx, state, data, 5*time.Minute)
	if err != nil {
		t.Fatalf("failed to set oauth state: %v", err)
	}

	// Launch concurrent goroutines trying to get the same state
	numGoroutines := 10
	results := make(chan string, numGoroutines)
	errors := make(chan error, numGoroutines)

	var wg sync.WaitGroup
	wg.Add(numGoroutines)

	for i := 0; i < numGoroutines; i++ {
		go func() {
			defer wg.Done()
			result, err := r.GetAndDeleteOAuthState(ctx, state)
			if err != nil {
				errors <- err
			} else {
				results <- result
			}
		}()
	}

	wg.Wait()
	close(results)
	close(errors)

	// Count successful retrievals
	successCount := 0
	for result := range results {
		if result == data {
			successCount++
		}
	}

	// Exactly one goroutine should have succeeded
	if successCount != 1 {
		t.Errorf("expected exactly 1 successful retrieval, got %d", successCount)
	}
}

// TestEnforceMaxSessionsAtomic tests that max sessions enforcement is atomic
func TestEnforceMaxSessionsAtomic(t *testing.T) {
	r, cleanup := setupTestRedis(t)
	defer cleanup()

	ctx := context.Background()
	userID := "test-user-123"
	maxSessions := 3

	// Create sessions
	for i := 0; i < 5; i++ {
		session := &RedisSession{
			ID:        "session-" + string(rune('a'+i)),
			UserID:    userID,
			IPAddress: "127.0.0.1",
			UserAgent: "test-agent",
			CreatedAt: time.Now().Add(time.Duration(i) * time.Second),
			ExpiresAt: time.Now().Add(24 * time.Hour),
		}
		token := "token-" + string(rune('a'+i))
		err := r.CreateSession(ctx, token, session, 24*time.Hour)
		if err != nil {
			t.Fatalf("failed to create session %d: %v", i, err)
		}
	}

	// Verify we have 5 sessions
	sessions, err := r.GetUserSessions(ctx, userID)
	if err != nil {
		t.Fatalf("failed to get user sessions: %v", err)
	}
	if len(sessions) != 5 {
		t.Errorf("expected 5 sessions before enforcement, got %d", len(sessions))
	}

	// Enforce max sessions
	err = r.EnforceMaxSessions(ctx, userID, maxSessions)
	if err != nil {
		t.Fatalf("failed to enforce max sessions: %v", err)
	}

	// Verify we now have maxSessions - 1 (to make room for new one)
	sessions, err = r.GetUserSessions(ctx, userID)
	if err != nil {
		t.Fatalf("failed to get user sessions after enforcement: %v", err)
	}
	if len(sessions) > maxSessions-1 {
		t.Errorf("expected at most %d sessions after enforcement, got %d", maxSessions-1, len(sessions))
	}
}

// TestEnforceMaxSessionsConcurrent tests that concurrent session enforcement
// doesn't exceed the limit
func TestEnforceMaxSessionsConcurrent(t *testing.T) {
	r, cleanup := setupTestRedis(t)
	defer cleanup()

	ctx := context.Background()
	userID := "concurrent-user"
	maxSessions := 5

	// Create initial sessions at the limit
	for i := 0; i < maxSessions; i++ {
		session := &RedisSession{
			ID:        "initial-" + string(rune('a'+i)),
			UserID:    userID,
			IPAddress: "127.0.0.1",
			UserAgent: "test-agent",
			CreatedAt: time.Now().Add(time.Duration(i) * time.Second),
			ExpiresAt: time.Now().Add(24 * time.Hour),
		}
		token := "initial-token-" + string(rune('a'+i))
		err := r.CreateSession(ctx, token, session, 24*time.Hour)
		if err != nil {
			t.Fatalf("failed to create initial session %d: %v", i, err)
		}
	}

	// Concurrently enforce max sessions (simulating concurrent logins)
	numGoroutines := 10
	var wg sync.WaitGroup
	wg.Add(numGoroutines)

	for i := 0; i < numGoroutines; i++ {
		go func(idx int) {
			defer wg.Done()
			err := r.EnforceMaxSessions(ctx, userID, maxSessions)
			if err != nil {
				t.Errorf("goroutine %d: failed to enforce max sessions: %v", idx, err)
			}
		}(i)
	}

	wg.Wait()

	// Final count should not exceed maxSessions
	sessions, err := r.GetUserSessions(ctx, userID)
	if err != nil {
		t.Fatalf("failed to get user sessions: %v", err)
	}
	if len(sessions) > maxSessions {
		t.Errorf("expected at most %d sessions, got %d", maxSessions, len(sessions))
	}
}

// TestTokenRevocation tests the token revocation functionality
func TestTokenRevocation(t *testing.T) {
	r, cleanup := setupTestRedis(t)
	defer cleanup()

	ctx := context.Background()
	tokenHash := "jti-abc123"
	ttl := 5 * time.Minute

	// Token should not be revoked initially
	revoked, err := r.IsTokenRevoked(ctx, tokenHash)
	if err != nil {
		t.Fatalf("failed to check revocation: %v", err)
	}
	if revoked {
		t.Error("token should not be revoked initially")
	}

	// Revoke the token
	err = r.RevokeToken(ctx, tokenHash, ttl)
	if err != nil {
		t.Fatalf("failed to revoke token: %v", err)
	}

	// Token should now be revoked
	revoked, err = r.IsTokenRevoked(ctx, tokenHash)
	if err != nil {
		t.Fatalf("failed to check revocation after revoke: %v", err)
	}
	if !revoked {
		t.Error("token should be revoked after RevokeToken call")
	}
}
