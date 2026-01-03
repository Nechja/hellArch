package store

import (
	"context"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
)

// WebhookFormat defines how the payload should be formatted when delivering
type WebhookFormat string

const (
	WebhookFormatRaw     WebhookFormat = "raw"     // Raw CloudEvent JSON
	WebhookFormatDiscord WebhookFormat = "discord" // Discord webhook format with embeds
	WebhookFormatSlack   WebhookFormat = "slack"   // Slack webhook format with blocks
)

type Webhook struct {
	ID         string        `json:"id"`
	Name       string        `json:"name"`
	URL        string        `json:"url"`
	Secret     string        `json:"secret,omitempty"`
	EventTypes []string      `json:"event_types"`
	Format     WebhookFormat `json:"format"`
	IsActive   bool          `json:"is_active"`
	CreatedAt  time.Time     `json:"created_at"`
	UpdatedAt  time.Time     `json:"updated_at"`
}

type CreateWebhookParams struct {
	Name       string
	URL        string
	Secret     string
	EventTypes []string
	Format     WebhookFormat
}

type UpdateWebhookParams struct {
	Name       *string
	URL        *string
	Secret     *string
	EventTypes []string
	Format     *WebhookFormat
	IsActive   *bool
}

func (s *Store) CreateWebhook(ctx context.Context, params CreateWebhookParams) (*Webhook, error) {
	format := params.Format
	if format == "" {
		format = WebhookFormatRaw
	}

	webhook := &Webhook{
		ID:         uuid.New().String(),
		Name:       params.Name,
		URL:        params.URL,
		Secret:     params.Secret,
		EventTypes: params.EventTypes,
		Format:     format,
		IsActive:   true,
		CreatedAt:  time.Now().UTC(),
		UpdatedAt:  time.Now().UTC(),
	}

	_, err := s.pool.Exec(ctx,
		`INSERT INTO webhooks (id, name, url, secret, event_types, format, is_active, created_at, updated_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
		webhook.ID, webhook.Name, webhook.URL, webhook.Secret, webhook.EventTypes,
		webhook.Format, webhook.IsActive, webhook.CreatedAt, webhook.UpdatedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to insert webhook: %w", err)
	}
	return webhook, nil
}

func (s *Store) GetWebhook(ctx context.Context, id string) (*Webhook, error) {
	var webhook Webhook
	err := s.pool.QueryRow(ctx,
		`SELECT id, name, url, secret, event_types, COALESCE(format, 'raw') as format, is_active, created_at, updated_at
		 FROM webhooks WHERE id = $1`, id,
	).Scan(&webhook.ID, &webhook.Name, &webhook.URL, &webhook.Secret,
		&webhook.EventTypes, &webhook.Format, &webhook.IsActive, &webhook.CreatedAt, &webhook.UpdatedAt)
	if err == pgx.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("failed to get webhook: %w", err)
	}
	return &webhook, nil
}

func (s *Store) ListWebhooks(ctx context.Context, limit, offset int) ([]*Webhook, int, error) {
	if limit <= 0 {
		limit = 50
	}

	var total int
	if err := s.pool.QueryRow(ctx, `SELECT COUNT(*) FROM webhooks`).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("failed to count webhooks: %w", err)
	}

	rows, err := s.pool.Query(ctx,
		`SELECT id, name, url, secret, event_types, COALESCE(format, 'raw') as format, is_active, created_at, updated_at
		 FROM webhooks ORDER BY created_at DESC LIMIT $1 OFFSET $2`,
		limit, offset,
	)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to query webhooks: %w", err)
	}
	defer rows.Close()

	var webhooks []*Webhook
	for rows.Next() {
		var webhook Webhook
		if err := rows.Scan(&webhook.ID, &webhook.Name, &webhook.URL, &webhook.Secret,
			&webhook.EventTypes, &webhook.Format, &webhook.IsActive, &webhook.CreatedAt, &webhook.UpdatedAt); err != nil {
			return nil, 0, fmt.Errorf("failed to scan webhook: %w", err)
		}
		webhooks = append(webhooks, &webhook)
	}
	return webhooks, total, nil
}

func (s *Store) GetWebhooksForEventType(ctx context.Context, eventType string) ([]*Webhook, error) {
	rows, err := s.pool.Query(ctx,
		`SELECT id, name, url, secret, event_types, COALESCE(format, 'raw') as format, is_active, created_at, updated_at
		 FROM webhooks WHERE is_active = true AND $1 = ANY(event_types)`,
		eventType,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to query webhooks for event type: %w", err)
	}
	defer rows.Close()

	var webhooks []*Webhook
	for rows.Next() {
		var webhook Webhook
		if err := rows.Scan(&webhook.ID, &webhook.Name, &webhook.URL, &webhook.Secret,
			&webhook.EventTypes, &webhook.Format, &webhook.IsActive, &webhook.CreatedAt, &webhook.UpdatedAt); err != nil {
			return nil, fmt.Errorf("failed to scan webhook: %w", err)
		}
		webhooks = append(webhooks, &webhook)
	}
	return webhooks, nil
}

func (s *Store) UpdateWebhook(ctx context.Context, id string, params UpdateWebhookParams) (*Webhook, error) {
	webhook, err := s.GetWebhook(ctx, id)
	if err != nil {
		return nil, err
	}
	if webhook == nil {
		return nil, nil
	}

	if params.Name != nil {
		webhook.Name = *params.Name
	}
	if params.URL != nil {
		webhook.URL = *params.URL
	}
	if params.Secret != nil {
		webhook.Secret = *params.Secret
	}
	if params.EventTypes != nil {
		webhook.EventTypes = params.EventTypes
	}
	if params.Format != nil {
		webhook.Format = *params.Format
	}
	if params.IsActive != nil {
		webhook.IsActive = *params.IsActive
	}
	webhook.UpdatedAt = time.Now().UTC()

	_, err = s.pool.Exec(ctx,
		`UPDATE webhooks SET name = $1, url = $2, secret = $3, event_types = $4, format = $5, is_active = $6, updated_at = $7
		 WHERE id = $8`,
		webhook.Name, webhook.URL, webhook.Secret, webhook.EventTypes, webhook.Format, webhook.IsActive, webhook.UpdatedAt, id,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to update webhook: %w", err)
	}
	return webhook, nil
}

func (s *Store) DeleteWebhook(ctx context.Context, id string) error {
	_, err := s.pool.Exec(ctx, `DELETE FROM webhooks WHERE id = $1`, id)
	if err != nil {
		return fmt.Errorf("failed to delete webhook: %w", err)
	}
	return nil
}
