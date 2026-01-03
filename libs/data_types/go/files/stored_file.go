package files

import "time"

// Represents a file stored in the object storage system.
type StoredFile struct {
	ID string `json:"id"`
	OwnerID string `json:"owner_id"`
	Bucket string `json:"bucket"`
	ObjectKey string `json:"object_key"`
	Filename string `json:"filename"`
	MimeType string `json:"mime_type"`
	SizeBytes int64 `json:"size_bytes"`
	ChecksumSha256 *string `json:"checksum_sha256,omitempty"`
	Metadata map[string]string `json:"metadata,omitempty"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
	DeletedAt *time.Time `json:"deleted_at,omitempty"`
	Variants []*FileVariant `json:"variants"`
}
