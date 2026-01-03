package files

import "time"

// Published when a file is deleted.
// Event type: hellarch.files.file.deleted.v1
type FileDeleted struct {
	FileID string `json:"file_id"`
	Filename string `json:"filename"`
	OwnerID string `json:"owner_id"`
	DeletedAt time.Time `json:"deleted_at"`
}
