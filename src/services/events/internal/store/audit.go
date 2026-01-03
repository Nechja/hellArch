package store

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
)

type AuditLog struct {
	ID        string          `json:"id"`
	EventType string          `json:"event_type"`
	EventID   string          `json:"event_id"`
	Source    string          `json:"source"`
	ActorID   *string         `json:"actor_id,omitempty"`
	TargetID  *string         `json:"target_id,omitempty"`
	Metadata  json.RawMessage `json:"metadata,omitempty"`
	CreatedAt time.Time       `json:"created_at"`
}

type ListAuditLogsParams struct {
	Limit     int
	Offset    int
	EventType string
	ActorID   string
}

func (s *Store) CreateAuditLog(ctx context.Context, log *AuditLog) error {
	if log.ID == "" {
		log.ID = uuid.New().String()
	}
	if log.CreatedAt.IsZero() {
		log.CreatedAt = time.Now().UTC()
	}

	_, err := s.pool.Exec(ctx,
		`INSERT INTO audit_logs (id, event_type, event_id, source, actor_id, target_id, metadata, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8)`,
		log.ID, log.EventType, log.EventID, log.Source, log.ActorID, log.TargetID, log.Metadata, log.CreatedAt,
	)
	if err != nil {
		return fmt.Errorf("failed to insert audit log: %w", err)
	}
	return nil
}

func (s *Store) ListAuditLogs(ctx context.Context, params ListAuditLogsParams) ([]*AuditLog, int, error) {
	if params.Limit <= 0 {
		params.Limit = 50
	}
	if params.Limit > 100 {
		params.Limit = 100
	}

	// Count total
	var total int
	countQuery := `SELECT COUNT(*) FROM audit_logs WHERE 1=1`
	countArgs := []any{}
	argNum := 1

	if params.EventType != "" {
		countQuery += fmt.Sprintf(" AND event_type = $%d", argNum)
		countArgs = append(countArgs, params.EventType)
		argNum++
	}
	if params.ActorID != "" {
		countQuery += fmt.Sprintf(" AND actor_id = $%d", argNum)
		countArgs = append(countArgs, params.ActorID)
		argNum++
	}

	if err := s.pool.QueryRow(ctx, countQuery, countArgs...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("failed to count audit logs: %w", err)
	}

	// Fetch results
	query := `SELECT id, event_type, event_id, source, actor_id, target_id, metadata, created_at
			  FROM audit_logs WHERE 1=1`
	args := []any{}
	argNum = 1

	if params.EventType != "" {
		query += fmt.Sprintf(" AND event_type = $%d", argNum)
		args = append(args, params.EventType)
		argNum++
	}
	if params.ActorID != "" {
		query += fmt.Sprintf(" AND actor_id = $%d", argNum)
		args = append(args, params.ActorID)
		argNum++
	}

	query += fmt.Sprintf(" ORDER BY created_at DESC LIMIT $%d OFFSET $%d", argNum, argNum+1)
	args = append(args, params.Limit, params.Offset)

	rows, err := s.pool.Query(ctx, query, args...)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to query audit logs: %w", err)
	}
	defer rows.Close()

	var logs []*AuditLog
	for rows.Next() {
		var log AuditLog
		if err := rows.Scan(&log.ID, &log.EventType, &log.EventID, &log.Source,
			&log.ActorID, &log.TargetID, &log.Metadata, &log.CreatedAt); err != nil {
			return nil, 0, fmt.Errorf("failed to scan audit log: %w", err)
		}
		logs = append(logs, &log)
	}

	return logs, total, nil
}
