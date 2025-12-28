package store

import (
	"context"
	"fmt"
	"time"

	"github.com/exaring/otelpgx"
	"github.com/jackc/pgx/v5/pgxpool"
)

type Postgres struct {
	pool *pgxpool.Pool
}

type User struct {
	ID        string
	Email     string
	Username  string
	CreatedAt time.Time
	UpdatedAt time.Time
}

type LinkedAccount struct {
	ID             string
	UserID         string
	Provider       string
	ProviderUserID string
	Email          *string
	Username       *string
	AvatarURL      *string
	AccessToken    *string
	RefreshToken   *string
	TokenExpiresAt *time.Time
	CreatedAt      time.Time
	UpdatedAt      time.Time
}

type Session struct {
	ID        string
	UserID    string
	Token     string
	IPAddress *string
	UserAgent *string
	ExpiresAt time.Time
	CreatedAt time.Time
}

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
	err := p.pool.QueryRow(ctx,
		`SELECT id, email, username, created_at, updated_at FROM users WHERE id = $1`,
		id,
	).Scan(&user.ID, &user.Email, &user.Username, &user.CreatedAt, &user.UpdatedAt)
	if err != nil {
		return nil, err
	}
	return &user, nil
}

func (p *Postgres) GetUserByProviderID(ctx context.Context, provider, providerUserID string) (*User, error) {
	var user User
	err := p.pool.QueryRow(ctx,
		`SELECT u.id, u.email, u.username, u.created_at, u.updated_at
		 FROM users u
		 JOIN linked_accounts la ON u.id = la.user_id
		 WHERE la.provider = $1 AND la.provider_user_id = $2`,
		provider, providerUserID,
	).Scan(&user.ID, &user.Email, &user.Username, &user.CreatedAt, &user.UpdatedAt)
	if err != nil {
		return nil, err
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

func (p *Postgres) CreateSession(ctx context.Context, session *Session) error {
	_, err := p.pool.Exec(ctx,
		`INSERT INTO sessions (id, user_id, token, ip_address, user_agent, expires_at)
		 VALUES ($1, $2, $3, $4, $5, $6)`,
		session.ID, session.UserID, session.Token, session.IPAddress, session.UserAgent, session.ExpiresAt,
	)
	return err
}

func (p *Postgres) GetSessionByToken(ctx context.Context, token string) (*Session, error) {
	var session Session
	err := p.pool.QueryRow(ctx,
		`SELECT id, user_id, token, ip_address, user_agent, expires_at, created_at
		 FROM sessions WHERE token = $1 AND expires_at > NOW()`,
		token,
	).Scan(&session.ID, &session.UserID, &session.Token, &session.IPAddress, &session.UserAgent, &session.ExpiresAt, &session.CreatedAt)
	if err != nil {
		return nil, err
	}
	return &session, nil
}

func (p *Postgres) DeleteSession(ctx context.Context, id string) error {
	_, err := p.pool.Exec(ctx, `DELETE FROM sessions WHERE id = $1`, id)
	return err
}

func (p *Postgres) DeleteSessionByToken(ctx context.Context, token string) error {
	_, err := p.pool.Exec(ctx, `DELETE FROM sessions WHERE token = $1`, token)
	return err
}
