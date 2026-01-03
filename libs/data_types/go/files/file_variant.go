package files

import "time"

// Represents a processed variant of a stored file (e.g., thumbnail, optimized version).
type FileVariant struct {
	ID string `json:"id"`
	FileID string `json:"file_id"`
	VariantType string `json:"variant_type"`
	ObjectKey string `json:"object_key"`
	MimeType string `json:"mime_type"`
	SizeBytes int64 `json:"size_bytes"`
	CreatedAt time.Time `json:"created_at"`
	File *StoredFile `json:"file,omitempty"`
}
