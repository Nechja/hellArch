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

// Session represents a user session stored in Redis
type RedisSession struct {
	ID        string    `json:"id"`
	UserID    string    `json:"user_id"`
	IPAddress string    `json:"ip_address"`
	UserAgent string    `json:"user_agent"`
	CreatedAt time.Time `json:"created_at"`
	ExpiresAt time.Time `json:"expires_at"`
}

// hashToken creates a SHA-256 hash of a token for secure storage
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

// GetAndDeleteOAuthState atomically retrieves and deletes OAuth state to prevent race conditions.
// This ensures the state can only be used once, even with concurrent requests.
var getAndDeleteStateScript = redis.NewScript(`
	local data = redis.call('GET', KEYS[1])
	if data then
		redis.call('DEL', KEYS[1])
	end
	return data
`)

func (r *Redis) GetAndDeleteOAuthState(ctx context.Context, state string) (string, error) {
	key := "oauth:state:" + state
	result, err := getAndDeleteStateScript.Run(ctx, r.client, []string{key}).Result()
	if err != nil {
		if err == redis.Nil {
			return "", redis.Nil
		}
		return "", err
	}
	if result == nil {
		return "", redis.Nil
	}
	return result.(string), nil
}

// Session management - Redis is the single source of truth
// Keys:
//   - session:{hashedToken} -> hash with session data
//   - user:{userID}:sessions -> set of hashed tokens
//   - session_id:{sessionID} -> hashed token (reverse lookup)

func (r *Redis) CreateSession(ctx context.Context, sessionToken string, session *RedisSession, ttl time.Duration) error {
	hashedToken := hashToken(sessionToken)
	sessionKey := "session:" + hashedToken
	userSessionsKey := "user:" + session.UserID + ":sessions"
	sessionIDKey := "session_id:" + session.ID

	pipe := r.client.Pipeline()

	pipe.HSet(ctx, sessionKey,
		"id", session.ID,
		"user_id", session.UserID,
		"ip_address", session.IPAddress,
		"user_agent", session.UserAgent,
		"created_at", session.CreatedAt.Format(time.RFC3339),
		"expires_at", session.ExpiresAt.Format(time.RFC3339),
		"token_hash", hashedToken,
	)
	pipe.Expire(ctx, sessionKey, ttl)

	pipe.SAdd(ctx, userSessionsKey, hashedToken)
	pipe.Expire(ctx, userSessionsKey, ttl)

	pipe.Set(ctx, sessionIDKey, hashedToken, ttl)

	_, err := pipe.Exec(ctx)
	return err
}

func (r *Redis) GetSession(ctx context.Context, sessionToken string) (*RedisSession, error) {
	hashedToken := hashToken(sessionToken)
	sessionKey := "session:" + hashedToken

	data, err := r.client.HGetAll(ctx, sessionKey).Result()
	if err != nil {
		return nil, err
	}
	if len(data) == 0 {
		return nil, redis.Nil
	}

	createdAt, _ := time.Parse(time.RFC3339, data["created_at"])
	expiresAt, _ := time.Parse(time.RFC3339, data["expires_at"])

	return &RedisSession{
		ID:        data["id"],
		UserID:    data["user_id"],
		IPAddress: data["ip_address"],
		UserAgent: data["user_agent"],
		CreatedAt: createdAt,
		ExpiresAt: expiresAt,
	}, nil
}

func (r *Redis) GetUserSessions(ctx context.Context, userID string) ([]*RedisSession, error) {
	userSessionsKey := "user:" + userID + ":sessions"

	tokenHashes, err := r.client.SMembers(ctx, userSessionsKey).Result()
	if err != nil {
		return nil, err
	}

	var sessions []*RedisSession
	for _, hashedToken := range tokenHashes {
		sessionKey := "session:" + hashedToken
		data, err := r.client.HGetAll(ctx, sessionKey).Result()
		if err != nil || len(data) == 0 {
			r.client.SRem(ctx, userSessionsKey, hashedToken)
			continue
		}

		createdAt, _ := time.Parse(time.RFC3339, data["created_at"])
		expiresAt, _ := time.Parse(time.RFC3339, data["expires_at"])

		sessions = append(sessions, &RedisSession{
			ID:        data["id"],
			UserID:    data["user_id"],
			IPAddress: data["ip_address"],
			UserAgent: data["user_agent"],
			CreatedAt: createdAt,
			ExpiresAt: expiresAt,
		})
	}

	return sessions, nil
}

func (r *Redis) DeleteSession(ctx context.Context, sessionToken string) error {
	hashedToken := hashToken(sessionToken)
	sessionKey := "session:" + hashedToken

	data, err := r.client.HMGet(ctx, sessionKey, "user_id", "id").Result()
	if err != nil && err != redis.Nil {
		return err
	}

	pipe := r.client.Pipeline()
	pipe.Del(ctx, sessionKey)

	if len(data) >= 2 {
		if userID, ok := data[0].(string); ok && userID != "" {
			pipe.SRem(ctx, "user:"+userID+":sessions", hashedToken)
		}
		if sessionID, ok := data[1].(string); ok && sessionID != "" {
			pipe.Del(ctx, "session_id:"+sessionID)
		}
	}

	_, err = pipe.Exec(ctx)
	return err
}

func (r *Redis) DeleteSessionByID(ctx context.Context, sessionID string) error {
	sessionIDKey := "session_id:" + sessionID

	hashedToken, err := r.client.Get(ctx, sessionIDKey).Result()
	if err != nil {
		return err
	}

	sessionKey := "session:" + hashedToken

	userID, _ := r.client.HGet(ctx, sessionKey, "user_id").Result()

	pipe := r.client.Pipeline()
	pipe.Del(ctx, sessionKey)
	pipe.Del(ctx, sessionIDKey)
	if userID != "" {
		pipe.SRem(ctx, "user:"+userID+":sessions", hashedToken)
	}

	_, err = pipe.Exec(ctx)
	return err
}

func (r *Redis) DeleteUserSessions(ctx context.Context, userID string) error {
	userSessionsKey := "user:" + userID + ":sessions"

	tokenHashes, err := r.client.SMembers(ctx, userSessionsKey).Result()
	if err != nil {
		return err
	}

	if len(tokenHashes) == 0 {
		return nil
	}

	pipe := r.client.Pipeline()
	for _, hashedToken := range tokenHashes {
		sessionKey := "session:" + hashedToken
		sessionID, _ := r.client.HGet(ctx, sessionKey, "id").Result()
		pipe.Del(ctx, sessionKey)
		if sessionID != "" {
			pipe.Del(ctx, "session_id:"+sessionID)
		}
	}
	pipe.Del(ctx, userSessionsKey)

	_, err = pipe.Exec(ctx)
	return err
}

// enforceMaxSessionsScript atomically enforces the max sessions limit.
// It retrieves all session hashes for a user, sorts them by creation time,
// and deletes the oldest sessions if the count exceeds the limit.
// This prevents race conditions where concurrent logins could exceed the limit.
var enforceMaxSessionsScript = redis.NewScript(`
	local userSessionsKey = KEYS[1]
	local maxSessions = tonumber(ARGV[1])

	-- Get all session token hashes for this user
	local tokenHashes = redis.call('SMEMBERS', userSessionsKey)
	local count = #tokenHashes

	if count < maxSessions then
		return 0
	end

	-- Collect sessions with their creation times
	local sessions = {}
	for _, hash in ipairs(tokenHashes) do
		local sessionKey = 'session:' .. hash
		local createdAt = redis.call('HGET', sessionKey, 'created_at')
		if createdAt then
			table.insert(sessions, {hash = hash, created_at = createdAt})
		else
			-- Session expired or invalid, remove from set
			redis.call('SREM', userSessionsKey, hash)
		end
	end

	-- Sort by created_at (oldest first)
	table.sort(sessions, function(a, b)
		return a.created_at < b.created_at
	end)

	-- Delete oldest sessions to make room for new one
	local toDelete = #sessions - maxSessions + 1
	local deleted = 0

	for i = 1, toDelete do
		if sessions[i] then
			local hash = sessions[i].hash
			local sessionKey = 'session:' .. hash
			local sessionId = redis.call('HGET', sessionKey, 'id')

			-- Delete session data
			redis.call('DEL', sessionKey)
			redis.call('SREM', userSessionsKey, hash)

			-- Delete reverse lookup
			if sessionId then
				redis.call('DEL', 'session_id:' .. sessionId)
			end

			deleted = deleted + 1
		end
	end

	return deleted
`)

func (r *Redis) EnforceMaxSessions(ctx context.Context, userID string, maxSessions int) error {
	userSessionsKey := "user:" + userID + ":sessions"
	_, err := enforceMaxSessionsScript.Run(ctx, r.client, []string{userSessionsKey}, maxSessions).Result()
	return err
}

func (r *Redis) CountActiveSessions(ctx context.Context) (int, error) {
	var count int
	var cursor uint64

	for {
		keys, nextCursor, err := r.client.Scan(ctx, cursor, "session:*", 100).Result()
		if err != nil {
			return 0, err
		}
		count += len(keys)
		cursor = nextCursor
		if cursor == 0 {
			break
		}
	}

	return count, nil
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
