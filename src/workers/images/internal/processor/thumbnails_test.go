package processor

import (
	"bytes"
	"image"
	"image/color"
	"image/jpeg"
	"image/png"
	"strings"
	"testing"
)

// createTestPNG creates a simple PNG image for testing
func createTestPNG(width, height int) []byte {
	img := image.NewRGBA(image.Rect(0, 0, width, height))
	// Fill with a solid color
	for y := 0; y < height; y++ {
		for x := 0; x < width; x++ {
			img.Set(x, y, color.RGBA{R: 255, G: 0, B: 0, A: 255})
		}
	}

	var buf bytes.Buffer
	png.Encode(&buf, img)
	return buf.Bytes()
}

// createTestJPEG creates a simple JPEG image for testing
func createTestJPEG(width, height int) []byte {
	img := image.NewRGBA(image.Rect(0, 0, width, height))
	for y := 0; y < height; y++ {
		for x := 0; x < width; x++ {
			img.Set(x, y, color.RGBA{R: 0, G: 255, B: 0, A: 255})
		}
	}

	var buf bytes.Buffer
	jpeg.Encode(&buf, img, &jpeg.Options{Quality: 90})
	return buf.Bytes()
}

func TestGenerateThumbnail(t *testing.T) {
	t.Run("generates thumbnail from PNG", func(t *testing.T) {
		input := createTestPNG(800, 600)

		result, err := GenerateThumbnail(input, 256)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		if len(result) == 0 {
			t.Error("expected non-empty result")
		}

		// Verify it's a valid JPEG
		img, format, err := image.DecodeConfig(bytes.NewReader(result))
		if err != nil {
			t.Fatalf("failed to decode result: %v", err)
		}
		if format != "jpeg" {
			t.Errorf("expected jpeg format, got %s", format)
		}

		// Verify dimensions are within maxSize
		if img.Width > 256 || img.Height > 256 {
			t.Errorf("thumbnail exceeds maxSize: %dx%d", img.Width, img.Height)
		}
	})

	t.Run("generates thumbnail from JPEG", func(t *testing.T) {
		input := createTestJPEG(1024, 768)

		result, err := GenerateThumbnail(input, 200)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		img, _, err := image.DecodeConfig(bytes.NewReader(result))
		if err != nil {
			t.Fatalf("failed to decode result: %v", err)
		}

		if img.Width > 200 || img.Height > 200 {
			t.Errorf("thumbnail exceeds maxSize: %dx%d", img.Width, img.Height)
		}
	})

	t.Run("preserves aspect ratio for landscape image", func(t *testing.T) {
		// 800x400 = 2:1 aspect ratio
		input := createTestPNG(800, 400)

		result, err := GenerateThumbnail(input, 200)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		img, _, _ := image.DecodeConfig(bytes.NewReader(result))

		// Should be 200x100 (maintaining 2:1 ratio)
		ratio := float64(img.Width) / float64(img.Height)
		expectedRatio := 2.0

		if ratio < expectedRatio-0.1 || ratio > expectedRatio+0.1 {
			t.Errorf("aspect ratio not preserved: got %dx%d (ratio %.2f), expected ~2:1",
				img.Width, img.Height, ratio)
		}
	})

	t.Run("preserves aspect ratio for portrait image", func(t *testing.T) {
		// 400x800 = 1:2 aspect ratio
		input := createTestPNG(400, 800)

		result, err := GenerateThumbnail(input, 200)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		img, _, _ := image.DecodeConfig(bytes.NewReader(result))

		// Should be 100x200 (maintaining 1:2 ratio)
		ratio := float64(img.Height) / float64(img.Width)
		expectedRatio := 2.0

		if ratio < expectedRatio-0.1 || ratio > expectedRatio+0.1 {
			t.Errorf("aspect ratio not preserved: got %dx%d (ratio %.2f), expected ~1:2",
				img.Width, img.Height, ratio)
		}
	})

	t.Run("handles square image", func(t *testing.T) {
		input := createTestPNG(500, 500)

		result, err := GenerateThumbnail(input, 100)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		img, _, _ := image.DecodeConfig(bytes.NewReader(result))

		if img.Width != img.Height {
			t.Errorf("square image should remain square: got %dx%d", img.Width, img.Height)
		}
		if img.Width > 100 {
			t.Errorf("thumbnail exceeds maxSize: %d", img.Width)
		}
	})

	t.Run("small image not upscaled", func(t *testing.T) {
		// Create image smaller than maxSize
		input := createTestPNG(50, 50)

		result, err := GenerateThumbnail(input, 256)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		img, _, _ := image.DecodeConfig(bytes.NewReader(result))

		// imaging.Fit doesn't upscale, so it should stay small
		if img.Width > 50 || img.Height > 50 {
			t.Errorf("small image should not be upscaled: got %dx%d", img.Width, img.Height)
		}
	})

	t.Run("returns error for invalid image data", func(t *testing.T) {
		invalidData := []byte("not an image")

		_, err := GenerateThumbnail(invalidData, 256)
		if err == nil {
			t.Error("expected error for invalid image data")
		}
	})

	t.Run("returns error for empty data", func(t *testing.T) {
		_, err := GenerateThumbnail([]byte{}, 256)
		if err == nil {
			t.Error("expected error for empty data")
		}
	})

	t.Run("returns error for truncated image", func(t *testing.T) {
		validPNG := createTestPNG(100, 100)
		truncated := validPNG[:len(validPNG)/2]

		_, err := GenerateThumbnail(truncated, 256)
		if err == nil {
			t.Error("expected error for truncated image")
		}
	})

	t.Run("error message includes format info", func(t *testing.T) {
		invalidData := []byte("not an image at all")

		_, err := GenerateThumbnail(invalidData, 256)
		if err == nil {
			t.Fatal("expected error")
		}

		if !strings.Contains(err.Error(), "failed to decode") {
			t.Errorf("error should mention decode failure: %v", err)
		}
	})

	t.Run("output is smaller than input for large images", func(t *testing.T) {
		input := createTestPNG(2000, 2000)

		result, err := GenerateThumbnail(input, 256)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		// Thumbnail should be significantly smaller
		if len(result) >= len(input) {
			t.Errorf("thumbnail (%d bytes) should be smaller than input (%d bytes)",
				len(result), len(input))
		}
	})

	t.Run("handles various maxSize values", func(t *testing.T) {
		input := createTestPNG(500, 500)

		sizes := []int{32, 64, 128, 256, 512}
		for _, size := range sizes {
			result, err := GenerateThumbnail(input, size)
			if err != nil {
				t.Errorf("maxSize %d: unexpected error: %v", size, err)
				continue
			}

			img, _, _ := image.DecodeConfig(bytes.NewReader(result))
			if img.Width > size || img.Height > size {
				t.Errorf("maxSize %d: thumbnail exceeds size: %dx%d", size, img.Width, img.Height)
			}
		}
	})

	t.Run("consistent output for same input", func(t *testing.T) {
		input := createTestPNG(300, 200)

		result1, _ := GenerateThumbnail(input, 100)
		result2, _ := GenerateThumbnail(input, 100)

		// Same input should produce same dimensions
		img1, _, _ := image.DecodeConfig(bytes.NewReader(result1))
		img2, _, _ := image.DecodeConfig(bytes.NewReader(result2))

		if img1.Width != img2.Width || img1.Height != img2.Height {
			t.Errorf("inconsistent dimensions: %dx%d vs %dx%d",
				img1.Width, img1.Height, img2.Width, img2.Height)
		}
	})
}

func TestGenerateThumbnailFormats(t *testing.T) {
	t.Run("always outputs JPEG regardless of input format", func(t *testing.T) {
		pngInput := createTestPNG(200, 200)

		result, err := GenerateThumbnail(pngInput, 100)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		// Check JPEG magic bytes
		if len(result) < 2 || result[0] != 0xFF || result[1] != 0xD8 {
			t.Error("output should be JPEG format (magic bytes FF D8)")
		}
	})

	t.Run("JPEG input produces JPEG output", func(t *testing.T) {
		jpegInput := createTestJPEG(200, 200)

		result, err := GenerateThumbnail(jpegInput, 100)
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}

		if len(result) < 2 || result[0] != 0xFF || result[1] != 0xD8 {
			t.Error("output should be JPEG format")
		}
	})
}

func BenchmarkGenerateThumbnail(b *testing.B) {
	input := createTestPNG(1920, 1080)

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		GenerateThumbnail(input, 256)
	}
}

func BenchmarkGenerateThumbnailSmall(b *testing.B) {
	input := createTestPNG(400, 300)

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		GenerateThumbnail(input, 128)
	}
}
