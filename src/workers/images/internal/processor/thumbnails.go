package processor

import (
	"bytes"
	"fmt"
	"image"
	"image/jpeg"

	"github.com/disintegration/imaging"
)

// GenerateThumbnail creates a thumbnail from image data, preserving aspect ratio.
// The maxSize parameter specifies the maximum dimension (width or height).
// Output is always JPEG format.
func GenerateThumbnail(data []byte, maxSize int) ([]byte, error) {
	// Decode the image
	img, format, err := image.Decode(bytes.NewReader(data))
	if err != nil {
		return nil, fmt.Errorf("failed to decode image (format: %s): %w", format, err)
	}

	// Resize to fit within maxSize x maxSize, preserving aspect ratio
	thumbnail := imaging.Fit(img, maxSize, maxSize, imaging.Lanczos)

	// Encode as JPEG
	var buf bytes.Buffer
	if err := jpeg.Encode(&buf, thumbnail, &jpeg.Options{Quality: 85}); err != nil {
		return nil, fmt.Errorf("failed to encode thumbnail: %w", err)
	}

	return buf.Bytes(), nil
}
