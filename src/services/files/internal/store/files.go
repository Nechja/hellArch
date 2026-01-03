package store

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
)

type StoredFile struct {
	ID             string            `json:"id"`
	OwnerID        string            `json:"owner_id"`
	Bucket         string            `json:"bucket"`
	ObjectKey      string            `json:"object_key"`
	Filename       string            `json:"filename"`
	MimeType       string            `json:"mime_type"`
	SizeBytes      int64             `json:"size_bytes"`
	ChecksumSha256 *string           `json:"checksum_sha256,omitempty"`
	Metadata       map[string]string `json:"metadata,omitempty"`
	CreatedAt      time.Time         `json:"created_at"`
	UpdatedAt      time.Time         `json:"updated_at"`
	DeletedAt      *time.Time        `json:"deleted_at,omitempty"`
}

type CreateFileParams struct {
	OwnerID        string
	Bucket         string
	ObjectKey      string
	Filename       string
	MimeType       string
	SizeBytes      int64
	ChecksumSha256 *string
	Metadata       map[string]string
}

type ListFilesParams struct {
	OwnerID string
	Limit   int
	Offset  int
}

func (s *Store) CreateFile(ctx context.Context, params CreateFileParams) (*StoredFile, error) {
	id := uuid.New().String()
	now := time.Now().UTC()

	var metadataJSON []byte
	if params.Metadata != nil {
		var err error
		metadataJSON, err = json.Marshal(params.Metadata)
		if err != nil {
			return nil, fmt.Errorf("failed to marshal metadata: %w", err)
		}
	}

	query := `
		INSERT INTO files (id, owner_id, bucket, object_key, filename, mime_type, size_bytes, checksum_sha256, metadata, created_at, updated_at)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
		RETURNING id, owner_id, bucket, object_key, filename, mime_type, size_bytes, checksum_sha256, metadata, created_at, updated_at, deleted_at
	`

	var file StoredFile
	var metadataBytes []byte
	err := s.pool.QueryRow(ctx, query,
		id, params.OwnerID, params.Bucket, params.ObjectKey, params.Filename,
		params.MimeType, params.SizeBytes, params.ChecksumSha256, metadataJSON, now, now,
	).Scan(
		&file.ID, &file.OwnerID, &file.Bucket, &file.ObjectKey, &file.Filename,
		&file.MimeType, &file.SizeBytes, &file.ChecksumSha256, &metadataBytes,
		&file.CreatedAt, &file.UpdatedAt, &file.DeletedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to create file: %w", err)
	}

	if metadataBytes != nil {
		json.Unmarshal(metadataBytes, &file.Metadata)
	}

	return &file, nil
}

func (s *Store) GetFile(ctx context.Context, id string) (*StoredFile, error) {
	query := `
		SELECT id, owner_id, bucket, object_key, filename, mime_type, size_bytes, checksum_sha256, metadata, created_at, updated_at, deleted_at
		FROM files
		WHERE id = $1 AND deleted_at IS NULL
	`

	var file StoredFile
	var metadataBytes []byte
	err := s.pool.QueryRow(ctx, query, id).Scan(
		&file.ID, &file.OwnerID, &file.Bucket, &file.ObjectKey, &file.Filename,
		&file.MimeType, &file.SizeBytes, &file.ChecksumSha256, &metadataBytes,
		&file.CreatedAt, &file.UpdatedAt, &file.DeletedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("failed to get file: %w", err)
	}

	if metadataBytes != nil {
		json.Unmarshal(metadataBytes, &file.Metadata)
	}

	return &file, nil
}

func (s *Store) ListFiles(ctx context.Context, params ListFilesParams) ([]StoredFile, error) {
	if params.Limit <= 0 {
		params.Limit = 50
	}

	query := `
		SELECT id, owner_id, bucket, object_key, filename, mime_type, size_bytes, checksum_sha256, metadata, created_at, updated_at, deleted_at
		FROM files
		WHERE owner_id = $1 AND deleted_at IS NULL
		ORDER BY created_at DESC
		LIMIT $2 OFFSET $3
	`

	rows, err := s.pool.Query(ctx, query, params.OwnerID, params.Limit, params.Offset)
	if err != nil {
		return nil, fmt.Errorf("failed to list files: %w", err)
	}
	defer rows.Close()

	var files []StoredFile
	for rows.Next() {
		var file StoredFile
		var metadataBytes []byte
		err := rows.Scan(
			&file.ID, &file.OwnerID, &file.Bucket, &file.ObjectKey, &file.Filename,
			&file.MimeType, &file.SizeBytes, &file.ChecksumSha256, &metadataBytes,
			&file.CreatedAt, &file.UpdatedAt, &file.DeletedAt,
		)
		if err != nil {
			return nil, fmt.Errorf("failed to scan file: %w", err)
		}

		if metadataBytes != nil {
			json.Unmarshal(metadataBytes, &file.Metadata)
		}

		files = append(files, file)
	}

	return files, nil
}

func (s *Store) SoftDeleteFile(ctx context.Context, id string) error {
	query := `UPDATE files SET deleted_at = $1, updated_at = $1 WHERE id = $2 AND deleted_at IS NULL`
	now := time.Now().UTC()

	result, err := s.pool.Exec(ctx, query, now, id)
	if err != nil {
		return fmt.Errorf("failed to delete file: %w", err)
	}

	if result.RowsAffected() == 0 {
		return fmt.Errorf("file not found")
	}

	return nil
}
