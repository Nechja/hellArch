package service

import (
	"context"
	"fmt"

	"github.com/minio/minio-go/v7"
	"github.com/nechja/hellarch/services/files/internal/storage"
	"github.com/nechja/hellarch/services/files/internal/store"
)

type Variant struct {
	store   *store.Store
	storage *storage.MinioStorage
}

func NewVariant(store *store.Store, storage *storage.MinioStorage) *Variant {
	return &Variant{
		store:   store,
		storage: storage,
	}
}

type CreateParams struct {
	FileID      string
	VariantType string
	ObjectKey   string
	MimeType    string
	SizeBytes   int64
}

func (v *Variant) Create(ctx context.Context, params CreateParams) (*store.FileVariant, error) {
	_, err := v.store.GetFile(ctx, params.FileID)
	if err != nil {
		return nil, fmt.Errorf("parent file not found: %w", err)
	}

	variant, err := v.store.CreateVariant(ctx, store.CreateVariantParams{
		FileID:      params.FileID,
		VariantType: params.VariantType,
		ObjectKey:   params.ObjectKey,
		MimeType:    params.MimeType,
		SizeBytes:   params.SizeBytes,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to create variant: %w", err)
	}

	return variant, nil
}

func (v *Variant) List(ctx context.Context, fileID string) ([]store.FileVariant, error) {
	variants, err := v.store.ListVariants(ctx, fileID)
	if err != nil {
		return nil, fmt.Errorf("failed to list variants: %w", err)
	}
	return variants, nil
}

type VariantDownloadResult struct {
	Reader  *minio.Object
	Variant *store.FileVariant
}

func (v *Variant) Download(ctx context.Context, fileID, variantType string) (*VariantDownloadResult, error) {
	variant, err := v.store.GetVariant(ctx, fileID, variantType)
	if err != nil {
		return nil, fmt.Errorf("variant not found: %w", err)
	}

	obj, err := v.storage.Download(ctx, variant.ObjectKey)
	if err != nil {
		return nil, fmt.Errorf("failed to download variant from storage: %w", err)
	}

	return &VariantDownloadResult{
		Reader:  obj,
		Variant: variant,
	}, nil
}
