package files

import "time"

// Published when file processing completes (e.g., thumbnail generation).
// Event type: hellarch.files.file.processed.v1
type FileProcessed struct {
	FileID string `json:"file_id"`
	VariantType string `json:"variant_type"`
	Status string `json:"status"`
	Error *string `json:"error,omitempty"`
	ProcessedAt time.Time `json:"processed_at"`
}
