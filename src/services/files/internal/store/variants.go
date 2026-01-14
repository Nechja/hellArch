package store

import (
	"context"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/nechja/hellarch/datatypes/files"
)

type FileVariant = files.FileVariant

type CreateVariantParams struct {
	FileID      string
	VariantType string
	ObjectKey   string
	MimeType    string
	SizeBytes   int64
}

func (s *Store) CreateVariant(ctx context.Context, params CreateVariantParams) (*FileVariant, error) {
	id := uuid.New().String()
	now := time.Now().UTC()

	query := `
		INSERT INTO file_variants (id, file_id, variant_type, object_key, mime_type, size_bytes, created_at)
		VALUES ($1, $2, $3, $4, $5, $6, $7)
		RETURNING id, file_id, variant_type, object_key, mime_type, size_bytes, created_at
	`

	var variant FileVariant
	err := s.pool.QueryRow(ctx, query,
		id, params.FileID, params.VariantType, params.ObjectKey, params.MimeType, params.SizeBytes, now,
	).Scan(
		&variant.ID, &variant.FileID, &variant.VariantType, &variant.ObjectKey,
		&variant.MimeType, &variant.SizeBytes, &variant.CreatedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to create variant: %w", err)
	}

	return &variant, nil
}

func (s *Store) GetVariant(ctx context.Context, fileID, variantType string) (*FileVariant, error) {
	query := `
		SELECT id, file_id, variant_type, object_key, mime_type, size_bytes, created_at
		FROM file_variants
		WHERE file_id = $1 AND variant_type = $2
	`

	var variant FileVariant
	err := s.pool.QueryRow(ctx, query, fileID, variantType).Scan(
		&variant.ID, &variant.FileID, &variant.VariantType, &variant.ObjectKey,
		&variant.MimeType, &variant.SizeBytes, &variant.CreatedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to get variant: %w", err)
	}

	return &variant, nil
}

func (s *Store) ListVariants(ctx context.Context, fileID string) ([]FileVariant, error) {
	query := `
		SELECT id, file_id, variant_type, object_key, mime_type, size_bytes, created_at
		FROM file_variants
		WHERE file_id = $1
		ORDER BY created_at ASC
	`

	rows, err := s.pool.Query(ctx, query, fileID)
	if err != nil {
		return nil, fmt.Errorf("failed to list variants: %w", err)
	}
	defer rows.Close()

	var variants []FileVariant
	for rows.Next() {
		var variant FileVariant
		err := rows.Scan(
			&variant.ID, &variant.FileID, &variant.VariantType, &variant.ObjectKey,
			&variant.MimeType, &variant.SizeBytes, &variant.CreatedAt,
		)
		if err != nil {
			return nil, fmt.Errorf("failed to scan variant: %w", err)
		}
		variants = append(variants, variant)
	}

	return variants, nil
}
