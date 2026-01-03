package storage

import (
	"context"
	"io"

	"github.com/minio/minio-go/v7"
	"github.com/minio/minio-go/v7/pkg/credentials"
)

type MinioConfig struct {
	Endpoint  string
	AccessKey string
	SecretKey string
	Bucket    string
	UseSSL    bool
}

type MinioStorage struct {
	client *minio.Client
	bucket string
}

func NewMinioStorage(ctx context.Context, cfg MinioConfig) (*MinioStorage, error) {
	client, err := minio.New(cfg.Endpoint, &minio.Options{
		Creds:  credentials.NewStaticV4(cfg.AccessKey, cfg.SecretKey, ""),
		Secure: cfg.UseSSL,
	})
	if err != nil {
		return nil, err
	}

	// Ensure bucket exists
	exists, err := client.BucketExists(ctx, cfg.Bucket)
	if err != nil {
		return nil, err
	}
	if !exists {
		if err := client.MakeBucket(ctx, cfg.Bucket, minio.MakeBucketOptions{}); err != nil {
			return nil, err
		}
	}

	return &MinioStorage{
		client: client,
		bucket: cfg.Bucket,
	}, nil
}

func (s *MinioStorage) Download(ctx context.Context, objectKey string) (io.ReadCloser, error) {
	return s.client.GetObject(ctx, s.bucket, objectKey, minio.GetObjectOptions{})
}

type UploadParams struct {
	ObjectKey   string
	Reader      io.Reader
	Size        int64
	ContentType string
}

func (s *MinioStorage) Upload(ctx context.Context, params UploadParams) error {
	_, err := s.client.PutObject(ctx, s.bucket, params.ObjectKey, params.Reader, params.Size, minio.PutObjectOptions{
		ContentType: params.ContentType,
	})
	return err
}

func (s *MinioStorage) Bucket() string {
	return s.bucket
}
