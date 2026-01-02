package store

import (
	"context"
	"fmt"
	"time"

	"github.com/exaring/otelpgx"
	"github.com/jackc/pgx/v5/pgxpool"
)

type Webhook struct {
	ID           string
	URL          string
	Secret       string
	EventTypes   []string
	IsActive     bool
	CreatedAt    time.Time
	UpdatedAt    time.Time
}

type WebhookDelivery struct {
	ID            string
	WebhookID     string
	EventID       string
	EventType     string
	Payload       []byte
	StatusCode    *int
	ResponseBody  *string
	Error         *string
	AttemptNumber int
	CreatedAt     time.Time
}

type Store struct {
	pool *pgxpool.Pool
}

func NewPostgres(ctx context.Context, databaseURL string) (*Store, error) {
	cfg, err := pgxpool.ParseConfig(databaseURL)
	if err != nil {
		return nil, fmt.Errorf("failed to parse database url: %w", err)
	}

	cfg.ConnConfig.Tracer = otelpgx.NewTracer()

	pool, err := pgxpool.NewWithConfig(ctx, cfg)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to database: %w", err)
	}

	if err := pool.Ping(ctx); err != nil {
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return &Store{pool: pool}, nil
}

func (s *Store) Close() {
	s.pool.Close()
}

func (s *Store) GetWebhooksForEventType(ctx context.Context, eventType string) ([]Webhook, error) {
	query := `
		SELECT id, url, secret, event_types, is_active, created_at, updated_at
		FROM webhooks
		WHERE is_active = true
		AND $1 = ANY(event_types)
	`

	rows, err := s.pool.Query(ctx, query, eventType)
	if err != nil {
		return nil, fmt.Errorf("failed to query webhooks: %w", err)
	}
	defer rows.Close()

	var webhooks []Webhook
	for rows.Next() {
		var w Webhook
		if err := rows.Scan(&w.ID, &w.URL, &w.Secret, &w.EventTypes, &w.IsActive, &w.CreatedAt, &w.UpdatedAt); err != nil {
			return nil, fmt.Errorf("failed to scan webhook: %w", err)
		}
		webhooks = append(webhooks, w)
	}

	return webhooks, nil
}

func (s *Store) InsertDelivery(ctx context.Context, d *WebhookDelivery) error {
	query := `
		INSERT INTO webhook_deliveries (webhook_id, event_id, event_type, payload, status_code, response_body, error, attempt_number, created_at)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
	`

	_, err := s.pool.Exec(ctx, query,
		d.WebhookID,
		d.EventID,
		d.EventType,
		d.Payload,
		d.StatusCode,
		d.ResponseBody,
		d.Error,
		d.AttemptNumber,
		d.CreatedAt,
	)
	if err != nil {
		return fmt.Errorf("failed to insert delivery: %w", err)
	}

	return nil
}
