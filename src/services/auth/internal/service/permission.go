package service

import (
	"context"

	"github.com/nechja/hellarch/auth/internal/store"
)

type Permission struct {
	postgres *store.Postgres
}

func NewPermission(postgres *store.Postgres) *Permission {
	return &Permission{
		postgres: postgres,
	}
}

func (p *Permission) Get(ctx context.Context, userID string) (map[string]bool, error) {
	return p.postgres.GetUserPermissions(ctx, userID)
}

func (p *Permission) Set(ctx context.Context, userID string, permissions map[string]bool) error {
	return p.postgres.SetUserPermissions(ctx, userID, permissions)
}

func (p *Permission) Grant(ctx context.Context, userID, permission string) error {
	return p.postgres.GrantPermission(ctx, userID, permission)
}

func (p *Permission) Revoke(ctx context.Context, userID, permission string) error {
	return p.postgres.RevokePermission(ctx, userID, permission)
}
