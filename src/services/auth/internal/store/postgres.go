package store

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	"github.com/exaring/otelpgx"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/nechja/hellarch/datatypes/auth"
)

type Postgres struct {
	pool *pgxpool.Pool
}

type User = auth.User
type LinkedAccount = auth.LinkedAccount

func NewPostgres(ctx context.Context, databaseURL string) (*Postgres, error) {
	cfg, err := pgxpool.ParseConfig(databaseURL)
	if err != nil {
		return nil, fmt.Errorf("failed to parse database URL: %w", err)
	}

	cfg.ConnConfig.Tracer = otelpgx.NewTracer()

	pool, err := pgxpool.NewWithConfig(ctx, cfg)
	if err != nil {
		return nil, fmt.Errorf("failed to create pool: %w", err)
	}

	if err := pool.Ping(ctx); err != nil {
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return &Postgres{pool: pool}, nil
}

func (p *Postgres) Close() {
	p.pool.Close()
}

func (p *Postgres) GetUserByID(ctx context.Context, id string) (*User, error) {
	var user User
	var permissionsJSON []byte
	err := p.pool.QueryRow(ctx,
		`SELECT id, email, username, is_active, COALESCE(permissions, '{}'::jsonb), created_at, updated_at FROM users WHERE id = $1`,
		id,
	).Scan(&user.ID, &user.Email, &user.Username, &user.IsActive, &permissionsJSON, &user.CreatedAt, &user.UpdatedAt)
	if err != nil {
		return nil, err
	}
	if len(permissionsJSON) > 0 {
		if err := json.Unmarshal(permissionsJSON, &user.Permissions); err != nil {
			slog.Error("failed to unmarshal user permissions", "error", err, "user_id", user.ID)
		}
	}
	return &user, nil
}

func (p *Postgres) GetUserByProviderID(ctx context.Context, provider, providerUserID string) (*User, error) {
	var user User
	var permissionsJSON []byte
	err := p.pool.QueryRow(ctx,
		`SELECT u.id, u.email, u.username, u.is_active, COALESCE(u.permissions, '{}'::jsonb), u.created_at, u.updated_at
		 FROM users u
		 JOIN linked_accounts la ON u.id = la.user_id
		 WHERE la.provider = $1 AND la.provider_user_id = $2`,
		provider, providerUserID,
	).Scan(&user.ID, &user.Email, &user.Username, &user.IsActive, &permissionsJSON, &user.CreatedAt, &user.UpdatedAt)
	if err != nil {
		return nil, err
	}
	if len(permissionsJSON) > 0 {
		if err := json.Unmarshal(permissionsJSON, &user.Permissions); err != nil {
			slog.Error("failed to unmarshal user permissions", "error", err, "user_id", user.ID)
		}
	}
	return &user, nil
}

func (p *Postgres) CreateUser(ctx context.Context, email, username string) (*User, error) {
	var user User
	err := p.pool.QueryRow(ctx,
		`INSERT INTO users (email, username) VALUES ($1, $2)
		 RETURNING id, email, username, created_at, updated_at`,
		email, username,
	).Scan(&user.ID, &user.Email, &user.Username, &user.CreatedAt, &user.UpdatedAt)
	if err != nil {
		return nil, err
	}
	return &user, nil
}

func (p *Postgres) CreateLinkedAccount(ctx context.Context, account *LinkedAccount) error {
	_, err := p.pool.Exec(ctx,
		`INSERT INTO linked_accounts (user_id, provider, provider_user_id, email, username, avatar_url)
		 VALUES ($1, $2, $3, $4, $5, $6)`,
		account.UserID, account.Provider, account.ProviderUserID, account.Email, account.Username, account.AvatarURL,
	)
	return err
}

func (p *Postgres) GetLinkedAccounts(ctx context.Context, userID string) ([]*LinkedAccount, error) {
	rows, err := p.pool.Query(ctx,
		`SELECT id, user_id, provider, provider_user_id, email, username, avatar_url, created_at, updated_at
		 FROM linked_accounts WHERE user_id = $1`,
		userID,
	)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var accounts []*LinkedAccount
	for rows.Next() {
		var account LinkedAccount
		if err := rows.Scan(
			&account.ID, &account.UserID, &account.Provider, &account.ProviderUserID,
			&account.Email, &account.Username, &account.AvatarURL, &account.CreatedAt, &account.UpdatedAt,
		); err != nil {
			return nil, err
		}
		accounts = append(accounts, &account)
	}
	return accounts, nil
}

// Admin methods

func (p *Postgres) ListUsers(ctx context.Context, limit, offset int) ([]*User, int, error) {
	var total int
	if err := p.pool.QueryRow(ctx, `SELECT COUNT(*) FROM users`).Scan(&total); err != nil {
		return nil, 0, err
	}

	rows, err := p.pool.Query(ctx,
		`SELECT id, email, username, is_active, COALESCE(permissions, '{}'::jsonb), created_at, updated_at FROM users ORDER BY created_at DESC LIMIT $1 OFFSET $2`,
		limit, offset,
	)
	if err != nil {
		return nil, 0, err
	}
	defer rows.Close()

	var users []*User
	for rows.Next() {
		var user User
		var permissionsJSON []byte
		if err := rows.Scan(&user.ID, &user.Email, &user.Username, &user.IsActive, &permissionsJSON, &user.CreatedAt, &user.UpdatedAt); err != nil {
			return nil, 0, err
		}
		if len(permissionsJSON) > 0 {
			if err := json.Unmarshal(permissionsJSON, &user.Permissions); err != nil {
				slog.Error("failed to unmarshal user permissions", "error", err, "user_id", user.ID)
			}
		}
		users = append(users, &user)
	}
	return users, total, nil
}

func (p *Postgres) CountUsers(ctx context.Context) (int, error) {
	var count int
	err := p.pool.QueryRow(ctx, `SELECT COUNT(*) FROM users`).Scan(&count)
	return count, err
}

func (p *Postgres) CountLinkedAccounts(ctx context.Context) (int, error) {
	var count int
	err := p.pool.QueryRow(ctx, `SELECT COUNT(*) FROM linked_accounts`).Scan(&count)
	return count, err
}

// Permission management methods

func (p *Postgres) GetUserPermissions(ctx context.Context, userID string) (map[string]bool, error) {
	var permissionsJSON []byte
	err := p.pool.QueryRow(ctx,
		`SELECT COALESCE(permissions, '{}'::jsonb) FROM users WHERE id = $1`,
		userID,
	).Scan(&permissionsJSON)
	if err != nil {
		return nil, err
	}
	var permissions map[string]bool
	if len(permissionsJSON) > 0 {
		if err := json.Unmarshal(permissionsJSON, &permissions); err != nil {
			slog.Error("failed to unmarshal user permissions", "error", err, "user_id", userID)
		}
	}
	if permissions == nil {
		permissions = make(map[string]bool)
	}
	return permissions, nil
}

func (p *Postgres) SetUserPermissions(ctx context.Context, userID string, permissions map[string]bool) error {
	permissionsJSON, err := json.Marshal(permissions)
	if err != nil {
		return fmt.Errorf("failed to marshal permissions: %w", err)
	}
	_, err = p.pool.Exec(ctx,
		`UPDATE users SET permissions = $1, updated_at = NOW() WHERE id = $2`,
		permissionsJSON, userID,
	)
	return err
}

func (p *Postgres) GrantPermission(ctx context.Context, userID, permission string) error {
	// Use explicit ARRAY constructor to ensure proper text[] conversion for jsonb_set path
	_, err := p.pool.Exec(ctx,
		`UPDATE users SET permissions = jsonb_set(COALESCE(permissions, '{}'::jsonb), ARRAY[$2]::text[], 'true'::jsonb), updated_at = NOW() WHERE id = $1`,
		userID, permission,
	)
	return err
}

func (p *Postgres) RevokePermission(ctx context.Context, userID, permission string) error {
	_, err := p.pool.Exec(ctx,
		`UPDATE users SET permissions = permissions - $2, updated_at = NOW() WHERE id = $1`,
		userID, permission,
	)
	return err
}

func (p *Postgres) SetUserActive(ctx context.Context, userID string, isActive bool) error {
	_, err := p.pool.Exec(ctx,
		`UPDATE users SET is_active = $1, updated_at = NOW() WHERE id = $2`,
		isActive, userID,
	)
	return err
}
