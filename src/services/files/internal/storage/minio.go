package storage

import (
	"context"
	"fmt"
	"io"
	"net/url"
	"time"

	"github.com/minio/minio-go/v7"
	"github.com/minio/minio-go/v7/pkg/credentials"
)

type MinioStorage struct {
	client *minio.Client
	bucket string
}

type MinioConfig struct {
	Endpoint  string
	AccessKey string
	SecretKey string
	Bucket    string
	UseSSL    bool
}

func NewMinioStorage(ctx context.Context, cfg MinioConfig) (*MinioStorage, error) {
	client, err := minio.New(cfg.Endpoint, &minio.Options{
		Creds:  credentials.NewStaticV4(cfg.AccessKey, cfg.SecretKey, ""),
		Secure: cfg.UseSSL,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to create minio client: %w", err)
	}

	// Ensure bucket exists
	exists, err := client.BucketExists(ctx, cfg.Bucket)
	if err != nil {
		return nil, fmt.Errorf("failed to check bucket: %w", err)
	}

	if !exists {
		err = client.MakeBucket(ctx, cfg.Bucket, minio.MakeBucketOptions{})
		if err != nil {
			return nil, fmt.Errorf("failed to create bucket: %w", err)
		}
	}

	return &MinioStorage{
		client: client,
		bucket: cfg.Bucket,
	}, nil
}

type UploadParams struct {
	ObjectKey   string
	Reader      io.Reader
	Size        int64
	ContentType string
}

func (s *MinioStorage) Upload(ctx context.Context, params UploadParams) (*minio.UploadInfo, error) {
	info, err := s.client.PutObject(ctx, s.bucket, params.ObjectKey, params.Reader, params.Size, minio.PutObjectOptions{
		ContentType: params.ContentType,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to upload object: %w", err)
	}
	return &info, nil
}

func (s *MinioStorage) Download(ctx context.Context, objectKey string) (*minio.Object, error) {
	obj, err := s.client.GetObject(ctx, s.bucket, objectKey, minio.GetObjectOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to get object: %w", err)
	}
	return obj, nil
}

func (s *MinioStorage) Delete(ctx context.Context, objectKey string) error {
	err := s.client.RemoveObject(ctx, s.bucket, objectKey, minio.RemoveObjectOptions{})
	if err != nil {
		return fmt.Errorf("failed to delete object: %w", err)
	}
	return nil
}

func (s *MinioStorage) PresignedUploadURL(ctx context.Context, objectKey string, expiry time.Duration) (*url.URL, error) {
	presignedURL, err := s.client.PresignedPutObject(ctx, s.bucket, objectKey, expiry)
	if err != nil {
		return nil, fmt.Errorf("failed to generate presigned upload URL: %w", err)
	}
	return presignedURL, nil
}

func (s *MinioStorage) PresignedDownloadURL(ctx context.Context, objectKey string, expiry time.Duration) (*url.URL, error) {
	presignedURL, err := s.client.PresignedGetObject(ctx, s.bucket, objectKey, expiry, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to generate presigned download URL: %w", err)
	}
	return presignedURL, nil
}

func (s *MinioStorage) Stat(ctx context.Context, objectKey string) (*minio.ObjectInfo, error) {
	info, err := s.client.StatObject(ctx, s.bucket, objectKey, minio.StatObjectOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to stat object: %w", err)
	}
	return &info, nil
}

func (s *MinioStorage) Bucket() string {
	return s.bucket
}
