package store

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
)

type DeadLetter struct {
	ID            string          `json:"id"`
	EventType     string          `json:"event_type"`
	EventID       string          `json:"event_id"`
	Source        string          `json:"source"`
	OriginalQueue string          `json:"original_queue"`
	DeathReason   string          `json:"death_reason"`
	RetryCount    int             `json:"retry_count"`
	Body          json.RawMessage `json:"body"`
	ErrorMessage  string          `json:"error_message,omitempty"`
	CreatedAt     time.Time       `json:"created_at"`
}

type ListDeadLettersParams struct {
	Limit     int
	Offset    int
	EventType string
}

func (s *Store) CreateDeadLetter(ctx context.Context, dl *DeadLetter) error {
	if dl.ID == "" {
		dl.ID = uuid.New().String()
	}
	if dl.CreatedAt.IsZero() {
		dl.CreatedAt = time.Now().UTC()
	}

	_, err := s.pool.Exec(ctx,
		`INSERT INTO dead_letters (id, event_type, event_id, source, original_queue, death_reason, retry_count, body, error_message, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)`,
		dl.ID, dl.EventType, dl.EventID, dl.Source, dl.OriginalQueue, dl.DeathReason, dl.RetryCount, dl.Body, dl.ErrorMessage, dl.CreatedAt,
	)
	if err != nil {
		return fmt.Errorf("failed to insert dead letter: %w", err)
	}
	return nil
}

func (s *Store) ListDeadLetters(ctx context.Context, params ListDeadLettersParams) ([]*DeadLetter, int, error) {
	if params.Limit <= 0 {
		params.Limit = 50
	}
	if params.Limit > 100 {
		params.Limit = 100
	}

	// Count total
	var total int
	countQuery := `SELECT COUNT(*) FROM dead_letters WHERE 1=1`
	countArgs := []any{}
	argNum := 1

	if params.EventType != "" {
		countQuery += fmt.Sprintf(" AND event_type = $%d", argNum)
		countArgs = append(countArgs, params.EventType)
		argNum++
	}

	if err := s.pool.QueryRow(ctx, countQuery, countArgs...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("failed to count dead letters: %w", err)
	}

	// Fetch results
	query := `SELECT id, event_type, event_id, source, original_queue, death_reason, retry_count, body, error_message, created_at
			  FROM dead_letters WHERE 1=1`
	args := []any{}
	argNum = 1

	if params.EventType != "" {
		query += fmt.Sprintf(" AND event_type = $%d", argNum)
		args = append(args, params.EventType)
		argNum++
	}

	query += fmt.Sprintf(" ORDER BY created_at DESC LIMIT $%d OFFSET $%d", argNum, argNum+1)
	args = append(args, params.Limit, params.Offset)

	rows, err := s.pool.Query(ctx, query, args...)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to query dead letters: %w", err)
	}
	defer rows.Close()

	var dls []*DeadLetter
	for rows.Next() {
		var dl DeadLetter
		if err := rows.Scan(&dl.ID, &dl.EventType, &dl.EventID, &dl.Source,
			&dl.OriginalQueue, &dl.DeathReason, &dl.RetryCount, &dl.Body, &dl.ErrorMessage, &dl.CreatedAt); err != nil {
			return nil, 0, fmt.Errorf("failed to scan dead letter: %w", err)
		}
		dls = append(dls, &dl)
	}

	return dls, total, nil
}

func (s *Store) DeleteDeadLetter(ctx context.Context, id string) error {
	result, err := s.pool.Exec(ctx, `DELETE FROM dead_letters WHERE id = $1`, id)
	if err != nil {
		return fmt.Errorf("failed to delete dead letter: %w", err)
	}
	if result.RowsAffected() == 0 {
		return fmt.Errorf("dead letter not found")
	}
	return nil
}
