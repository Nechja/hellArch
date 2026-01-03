package processor

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"strings"
	"time"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/files"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/workers/images/internal/storage"
)

type Processor struct {
	storage       *storage.MinioStorage
	publisher     *rabbitmq.Publisher
	filesAPIURL   string
	thumbnailSize int
	httpClient    *http.Client
}

type Config struct {
	Storage       *storage.MinioStorage
	Publisher     *rabbitmq.Publisher
	FilesAPIURL   string
	ThumbnailSize int
}

func New(cfg Config) *Processor {
	return &Processor{
		storage:       cfg.Storage,
		publisher:     cfg.Publisher,
		filesAPIURL:   cfg.FilesAPIURL,
		thumbnailSize: cfg.ThumbnailSize,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

func (p *Processor) Process(ctx context.Context, eventType string, data []byte) error {
	if eventType != cloudevents.EventTypeFileUploaded {
		return nil
	}

	var event files.FileUploaded
	if err := json.Unmarshal(data, &event); err != nil {
		return fmt.Errorf("failed to unmarshal file uploaded event: %w", err)
	}

	// Only process images
	if !strings.HasPrefix(event.MimeType, "image/") {
		slog.Debug("skipping non-image file", "file_id", event.FileID, "mime_type", event.MimeType)
		return nil
	}

	slog.Info("processing image", "file_id", event.FileID, "mime_type", event.MimeType)

	// Get the object key from the files API
	objectKey, err := p.getObjectKey(ctx, event.FileID)
	if err != nil {
		return p.publishFailure(ctx, event.FileID, "thumbnail", fmt.Sprintf("failed to get object key: %v", err))
	}

	// Download the original image
	reader, err := p.storage.Download(ctx, objectKey)
	if err != nil {
		return p.publishFailure(ctx, event.FileID, "thumbnail", fmt.Sprintf("failed to download: %v", err))
	}
	defer reader.Close()

	// Read image data
	imageData, err := io.ReadAll(reader)
	if err != nil {
		return p.publishFailure(ctx, event.FileID, "thumbnail", fmt.Sprintf("failed to read image: %v", err))
	}

	// Generate thumbnail
	thumbnail, err := GenerateThumbnail(imageData, p.thumbnailSize)
	if err != nil {
		return p.publishFailure(ctx, event.FileID, "thumbnail", fmt.Sprintf("failed to generate thumbnail: %v", err))
	}

	// Upload thumbnail
	thumbKey := objectKey + "_thumb.jpg"
	if err := p.storage.Upload(ctx, storage.UploadParams{
		ObjectKey:   thumbKey,
		Reader:      bytes.NewReader(thumbnail),
		Size:        int64(len(thumbnail)),
		ContentType: "image/jpeg",
	}); err != nil {
		return p.publishFailure(ctx, event.FileID, "thumbnail", fmt.Sprintf("failed to upload thumbnail: %v", err))
	}

	// Create variant record via files API
	if err := p.createVariant(ctx, event.FileID, "thumbnail", thumbKey, "image/jpeg", int64(len(thumbnail))); err != nil {
		slog.Warn("failed to create variant record", "error", err)
	}

	// Publish success event
	return p.publishSuccess(ctx, event.FileID, "thumbnail")
}

func (p *Processor) getObjectKey(ctx context.Context, fileID string) (string, error) {
	url := fmt.Sprintf("%s/api/files/%s/meta", p.filesAPIURL, fileID)
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return "", err
	}

	resp, err := p.httpClient.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return "", fmt.Errorf("files API returned status %d", resp.StatusCode)
	}

	var result struct {
		ObjectKey string `json:"object_key"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", err
	}

	return result.ObjectKey, nil
}

func (p *Processor) createVariant(ctx context.Context, fileID, variantType, objectKey, mimeType string, size int64) error {
	url := fmt.Sprintf("%s/api/files/%s/variants", p.filesAPIURL, fileID)
	body := map[string]any{
		"variant_type": variantType,
		"object_key":   objectKey,
		"mime_type":    mimeType,
		"size_bytes":   size,
	}
	jsonBody, _ := json.Marshal(body)

	req, err := http.NewRequestWithContext(ctx, http.MethodPost, url, bytes.NewReader(jsonBody))
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := p.httpClient.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated {
		return fmt.Errorf("files API returned status %d", resp.StatusCode)
	}

	return nil
}

func (p *Processor) publishSuccess(ctx context.Context, fileID, variantType string) error {
	if p.publisher == nil {
		return nil
	}

	event := files.FileProcessed{
		FileID:      fileID,
		VariantType: variantType,
		Status:      "success",
		ProcessedAt: time.Now(),
	}
	return p.publisher.Publish(ctx, cloudevents.EventTypeFileProcessed, event)
}

func (p *Processor) publishFailure(ctx context.Context, fileID, variantType, errMsg string) error {
	slog.Error("image processing failed", "file_id", fileID, "variant_type", variantType, "error", errMsg)

	if p.publisher == nil {
		return nil
	}

	event := files.FileProcessed{
		FileID:      fileID,
		VariantType: variantType,
		Status:      "failed",
		Error:       &errMsg,
		ProcessedAt: time.Now(),
	}
	return p.publisher.Publish(ctx, cloudevents.EventTypeFileProcessed, event)
}
