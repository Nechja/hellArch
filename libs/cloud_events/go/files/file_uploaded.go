package files

import "time"

// Published when a file is successfully uploaded.
// Event type: hellarch.files.file.uploaded.v1
type FileUploaded struct {
	FileID string `json:"file_id"`
	Filename string `json:"filename"`
	MimeType string `json:"mime_type"`
	SizeBytes int64 `json:"size_bytes"`
	OwnerID string `json:"owner_id"`
	ChecksumSha256 *string `json:"checksum_sha256,omitempty"`
	UploadedAt time.Time `json:"uploaded_at"`
}
