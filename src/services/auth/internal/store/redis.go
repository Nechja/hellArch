package store

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"time"

	"github.com/redis/go-redis/extra/redisotel/v9"
	"github.com/redis/go-redis/v9"
)

// hashToken creates a SHA-256 hash of a token for secure storage
// This prevents token exposure if Redis is compromised
func hashToken(token string) string {
	h := sha256.Sum256([]byte(token))
	return hex.EncodeToString(h[:])
}

type Redis struct {
	client *redis.Client
}

func NewRedis(ctx context.Context, redisURL string) (*Redis, error) {
	opt, err := redis.ParseURL(redisURL)
	if err != nil {
		return nil, fmt.Errorf("failed to parse redis URL: %w", err)
	}

	client := redis.NewClient(opt)

	// Enable OTel tracing
	if err := redisotel.InstrumentTracing(client); err != nil {
		return nil, fmt.Errorf("failed to instrument redis tracing: %w", err)
	}

	if err := client.Ping(ctx).Err(); err != nil {
		return nil, fmt.Errorf("failed to ping redis: %w", err)
	}

	return &Redis{client: client}, nil
}

func (r *Redis) Close() error {
	return r.client.Close()
}

// OAuth state management
func (r *Redis) SetOAuthState(ctx context.Context, state string, data string, ttl time.Duration) error {
	return r.client.Set(ctx, "oauth:state:"+state, data, ttl).Err()
}

func (r *Redis) GetOAuthState(ctx context.Context, state string) (string, error) {
	return r.client.Get(ctx, "oauth:state:"+state).Result()
}

func (r *Redis) DeleteOAuthState(ctx context.Context, state string) error {
	return r.client.Del(ctx, "oauth:state:"+state).Err()
}

// Session cache - tokens are hashed before storage for security
func (r *Redis) CacheSession(ctx context.Context, sessionToken string, userID string, ttl time.Duration) error {
	hashedToken := hashToken(sessionToken)
	return r.client.Set(ctx, "session:"+hashedToken, userID, ttl).Err()
}

func (r *Redis) GetCachedSession(ctx context.Context, sessionToken string) (string, error) {
	hashedToken := hashToken(sessionToken)
	return r.client.Get(ctx, "session:"+hashedToken).Result()
}

func (r *Redis) InvalidateSession(ctx context.Context, sessionToken string) error {
	hashedToken := hashToken(sessionToken)
	return r.client.Del(ctx, "session:"+hashedToken).Err()
}

// Token revocation list
func (r *Redis) RevokeToken(ctx context.Context, tokenHash string, ttl time.Duration) error {
	return r.client.Set(ctx, "revoked:"+tokenHash, "1", ttl).Err()
}

func (r *Redis) IsTokenRevoked(ctx context.Context, tokenHash string) (bool, error) {
	result, err := r.client.Exists(ctx, "revoked:"+tokenHash).Result()
	if err != nil {
		return false, err
	}
	return result > 0, nil
}
