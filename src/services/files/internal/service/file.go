package service

import (
	"context"
	"fmt"
	"io"
	"log/slog"
	"net/url"
	"time"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/cloudevents/files"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
	"github.com/minio/minio-go/v7"
)

type File struct {
	store     *store.Store
	storage   *storage.MinioStorage
	publisher *rabbitmq.Publisher
}

func NewFile(
	store *store.Store,
	storage *storage.MinioStorage,
	publisher *rabbitmq.Publisher,
) *File {
	return &File{
		store:     store,
		storage:   storage,
		publisher: publisher,
	}
}

func (f *File) Get(ctx context.Context, id string) (*store.StoredFile, error) {
	file, err := f.store.GetFile(ctx, id)
	if err != nil {
		return nil, fmt.Errorf("file not found: %w", err)
	}
	return file, nil
}

func (f *File) List(ctx context.Context, ownerID string, limit, offset int) ([]store.StoredFile, error) {
	if limit <= 0 {
		limit = 50
	}

	filesList, err := f.store.ListFiles(ctx, store.ListFilesParams{
		OwnerID: ownerID,
		Limit:   limit,
		Offset:  offset,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to list files: %w", err)
	}
	return filesList, nil
}

func (f *File) Delete(ctx context.Context, id string) error {
	file, err := f.store.GetFile(ctx, id)
	if err != nil {
		return fmt.Errorf("file not found: %w", err)
	}

	if err := f.store.SoftDeleteFile(ctx, id); err != nil {
		return fmt.Errorf("failed to delete file: %w", err)
	}

	f.publishDeleted(ctx, file)

	return nil
}

type DownloadResult struct {
	Reader   *minio.Object
	File     *store.StoredFile
}

func (f *File) Download(ctx context.Context, id string) (*DownloadResult, error) {
	file, err := f.store.GetFile(ctx, id)
	if err != nil {
		return nil, fmt.Errorf("file not found: %w", err)
	}

	obj, err := f.storage.Download(ctx, file.ObjectKey)
	if err != nil {
		return nil, fmt.Errorf("failed to download from storage: %w", err)
	}

	return &DownloadResult{
		Reader: obj,
		File:   file,
	}, nil
}

type PresignDownloadResult struct {
	URL       *url.URL
	Filename  string
	ExpiresAt time.Time
}

func (f *File) PresignDownload(ctx context.Context, id string) (*PresignDownloadResult, error) {
	file, err := f.store.GetFile(ctx, id)
	if err != nil {
		return nil, fmt.Errorf("file not found: %w", err)
	}

	expiry := 15 * time.Minute
	presignedURL, err := f.storage.PresignedDownloadURL(ctx, file.ObjectKey, expiry)
	if err != nil {
		return nil, fmt.Errorf("failed to generate presigned URL: %w", err)
	}

	return &PresignDownloadResult{
		URL:       presignedURL,
		Filename:  file.Filename,
		ExpiresAt: time.Now().Add(expiry).UTC(),
	}, nil
}

func (f *File) StreamTo(w io.Writer, reader *minio.Object) error {
	_, err := io.Copy(w, reader)
	return err
}

func (f *File) publishDeleted(ctx context.Context, file *store.StoredFile) {
	if f.publisher == nil {
		return
	}

	event := files.FileDeleted{
		FileID:    file.ID,
		Filename:  file.Filename,
		OwnerID:   file.OwnerID,
		DeletedAt: time.Now(),
	}
	if err := f.publisher.Publish(ctx, cloudevents.EventTypeFileDeleted, event); err != nil {
		slog.Warn("failed to publish file deleted event", "error", err, "file_id", file.ID)
	}
}
