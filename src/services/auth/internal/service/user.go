package service

import (
	"context"
	"errors"

	"github.com/jackc/pgx/v5"
	"github.com/nechja/hellarch/auth/internal/events"
	"github.com/nechja/hellarch/auth/internal/oauth"
	"github.com/nechja/hellarch/auth/internal/store"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type User struct {
	postgres  *store.Postgres
	publisher *events.Publisher
}

func NewUser(postgres *store.Postgres, publisher *events.Publisher) *User {
	return &User{
		postgres:  postgres,
		publisher: publisher,
	}
}

type FindOrCreateResult struct {
	User      *store.User
	IsNewUser bool
}

func (u *User) FindOrCreateFromOAuth(ctx context.Context, info *oauth.UserInfo) (*FindOrCreateResult, error) {
	// First, check if this OAuth account is already linked
	user, err := u.postgres.GetUserByProviderID(ctx, string(info.Provider), info.ID)
	if err == nil {
		return &FindOrCreateResult{User: user, IsNewUser: false}, nil
	}

	if !errors.Is(err, pgx.ErrNoRows) {
		return nil, status.Error(codes.Internal, "failed to lookup user by provider")
	}

	user, err = u.postgres.GetUserByEmail(ctx, info.Email)
	if err == nil {
		// Return generic error to prevent account enumeration
		// Don't reveal that an account with this email already exists
		return nil, status.Error(codes.InvalidArgument, "registration_failed")
	}

	if !errors.Is(err, pgx.ErrNoRows) {
		return nil, status.Error(codes.Internal, "failed to lookup user by email")
	}

	// No user exists - create new user and link account
	user, err = u.postgres.CreateUser(ctx, info.Email, info.Username)
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to create user")
	}

	err = u.postgres.CreateLinkedAccount(ctx, &store.LinkedAccount{
		UserID:         user.ID,
		Provider:       string(info.Provider),
		ProviderUserID: info.ID,
		Email:          ptr(info.Email),
		Username:       ptr(info.Username),
		AvatarURL:      ptr(info.AvatarURL),
	})
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to link account")
	}

	return &FindOrCreateResult{User: user, IsNewUser: true}, nil
}

func (u *User) PublishCreated(ctx context.Context, user *store.User, info *oauth.UserInfo) error {
	return u.publisher.PublishUserCreated(ctx, events.UserCreated{
		UserID:         user.ID,
		Email:          user.Email,
		Username:       info.Username,
		Provider:       string(info.Provider),
		ProviderUserID: info.ID,
		AvatarURL:      info.AvatarURL,
		CreatedAt:      user.CreatedAt,
	})
}

func (u *User) LinkProviderToUser(ctx context.Context, userID string, info *oauth.UserInfo) (*FindOrCreateResult, error) {
	user, err := u.postgres.GetUserByID(ctx, userID)
	if err != nil {
		return nil, status.Error(codes.NotFound, "user not found")
	}

	existingOwner, err := u.postgres.GetUserByProviderID(ctx, string(info.Provider), info.ID)
	if err == nil {
		if existingOwner.ID == userID {
			return &FindOrCreateResult{User: user, IsNewUser: false}, nil
		}
		return nil, status.Error(codes.AlreadyExists, "provider_linked_to_other_account")
	}

	// Link the provider to this user
	err = u.postgres.CreateLinkedAccount(ctx, &store.LinkedAccount{
		UserID:         user.ID,
		Provider:       string(info.Provider),
		ProviderUserID: info.ID,
		Email:          ptr(info.Email),
		Username:       ptr(info.Username),
		AvatarURL:      ptr(info.AvatarURL),
	})
	if err != nil {
		return nil, status.Error(codes.Internal, "failed to link account")
	}

	return &FindOrCreateResult{User: user, IsNewUser: false}, nil
}

func (u *User) FindByID(ctx context.Context, id string) (*store.User, error) {
	user, err := u.postgres.GetUserByID(ctx, id)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, status.Error(codes.NotFound, "user not found")
		}
		return nil, status.Error(codes.Internal, "failed to get user")
	}
	return user, nil
}

func (u *User) List(ctx context.Context, limit, offset int) ([]*store.User, int, error) {
	return u.postgres.ListUsers(ctx, limit, offset)
}

func (u *User) GetLinkedAccounts(ctx context.Context, userID string) ([]*store.LinkedAccount, error) {
	return u.postgres.GetLinkedAccounts(ctx, userID)
}

func (u *User) Activate(ctx context.Context, id string) error {
	return u.postgres.SetUserActive(ctx, id, true)
}

func (u *User) Deactivate(ctx context.Context, id string) error {
	return u.postgres.SetUserActive(ctx, id, false)
}

func (u *User) Count(ctx context.Context) (int, error) {
	return u.postgres.CountUsers(ctx)
}

func (u *User) CountLinkedAccounts(ctx context.Context) (int, error) {
	return u.postgres.CountLinkedAccounts(ctx)
}

func ptr(s string) *string {
	return &s
}
