package store

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
)

type WebhookDelivery struct {
	ID           string          `json:"id"`
	WebhookID    string          `json:"webhook_id"`
	EventID      string          `json:"event_id"`
	EventType    string          `json:"event_type"`
	Payload      json.RawMessage `json:"payload,omitempty"`
	StatusCode   int             `json:"status_code"`
	ResponseBody *string         `json:"response_body,omitempty"`
	Success      bool            `json:"success"`
	Attempt      int             `json:"attempt"`
	CreatedAt    time.Time       `json:"created_at"`
}

type CreateDeliveryParams struct {
	WebhookID    string
	EventID      string
	EventType    string
	Payload      json.RawMessage
	StatusCode   int
	ResponseBody *string
	Success      bool
	Attempt      int
}

func (s *Store) CreateDelivery(ctx context.Context, params CreateDeliveryParams) (*WebhookDelivery, error) {
	delivery := &WebhookDelivery{
		ID:           uuid.New().String(),
		WebhookID:    params.WebhookID,
		EventID:      params.EventID,
		EventType:    params.EventType,
		Payload:      params.Payload,
		StatusCode:   params.StatusCode,
		ResponseBody: params.ResponseBody,
		Success:      params.Success,
		Attempt:      params.Attempt,
		CreatedAt:    time.Now().UTC(),
	}

	_, err := s.pool.Exec(ctx,
		`INSERT INTO webhook_deliveries (id, webhook_id, event_id, event_type, payload, status_code, response_body, success, attempt, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)`,
		delivery.ID, delivery.WebhookID, delivery.EventID, delivery.EventType,
		delivery.Payload, delivery.StatusCode, delivery.ResponseBody, delivery.Success,
		delivery.Attempt, delivery.CreatedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to insert delivery: %w", err)
	}
	return delivery, nil
}

type ListDeliveriesParams struct {
	Limit     int
	Offset    int
	WebhookID string
	EventID   string
	Success   *bool
}

func (s *Store) ListDeliveries(ctx context.Context, params ListDeliveriesParams) ([]*WebhookDelivery, int, error) {
	if params.Limit <= 0 {
		params.Limit = 50
	}
	if params.Limit > 100 {
		params.Limit = 100
	}

	// Count total
	var total int
	countQuery := `SELECT COUNT(*) FROM webhook_deliveries WHERE 1=1`
	countArgs := []any{}
	argNum := 1

	if params.WebhookID != "" {
		countQuery += fmt.Sprintf(" AND webhook_id = $%d", argNum)
		countArgs = append(countArgs, params.WebhookID)
		argNum++
	}
	if params.EventID != "" {
		countQuery += fmt.Sprintf(" AND event_id = $%d", argNum)
		countArgs = append(countArgs, params.EventID)
		argNum++
	}
	if params.Success != nil {
		countQuery += fmt.Sprintf(" AND success = $%d", argNum)
		countArgs = append(countArgs, *params.Success)
		argNum++
	}

	if err := s.pool.QueryRow(ctx, countQuery, countArgs...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("failed to count deliveries: %w", err)
	}

	// Fetch results
	query := `SELECT id, webhook_id, event_id, event_type, payload, status_code, response_body, success, attempt, created_at
			  FROM webhook_deliveries WHERE 1=1`
	args := []any{}
	argNum = 1

	if params.WebhookID != "" {
		query += fmt.Sprintf(" AND webhook_id = $%d", argNum)
		args = append(args, params.WebhookID)
		argNum++
	}
	if params.EventID != "" {
		query += fmt.Sprintf(" AND event_id = $%d", argNum)
		args = append(args, params.EventID)
		argNum++
	}
	if params.Success != nil {
		query += fmt.Sprintf(" AND success = $%d", argNum)
		args = append(args, *params.Success)
		argNum++
	}

	query += fmt.Sprintf(" ORDER BY created_at DESC LIMIT $%d OFFSET $%d", argNum, argNum+1)
	args = append(args, params.Limit, params.Offset)

	rows, err := s.pool.Query(ctx, query, args...)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to query deliveries: %w", err)
	}
	defer rows.Close()

	var deliveries []*WebhookDelivery
	for rows.Next() {
		var d WebhookDelivery
		if err := rows.Scan(&d.ID, &d.WebhookID, &d.EventID, &d.EventType,
			&d.Payload, &d.StatusCode, &d.ResponseBody, &d.Success, &d.Attempt, &d.CreatedAt); err != nil {
			return nil, 0, fmt.Errorf("failed to scan delivery: %w", err)
		}
		deliveries = append(deliveries, &d)
	}

	return deliveries, total, nil
}
