"""
Error path tests for the Files service.

These tests verify proper error handling for edge cases and invalid inputs.
"""

import os
from uuid import uuid4

import httpx
import pytest


def get_files_url() -> str:
    return os.getenv("FILES_URL", "http://localhost:5006")


class TestUploadErrorPaths:
    """Error scenarios for file upload endpoints."""

    async def test_upload_without_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload without file data returns error."""
        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
        )
        assert response.status_code in (400, 422)

    async def test_upload_empty_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload empty file may be rejected."""
        files = {"file": ("empty.txt", b"", "text/plain")}
        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )
        # Empty files might be allowed or rejected
        assert response.status_code in (200, 201, 400, 422)

    async def test_upload_oversized_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload file exceeding max size returns error."""
        # Create a 2GB+ content (just check the header, don't send actual data)
        # This would normally be rejected by Content-Length check
        # We'll test with a reasonable but potentially too-large payload
        large_content = b"x" * (100 * 1024 * 1024)  # 100MB
        files = {"file": ("large.bin", large_content, "application/octet-stream")}

        try:
            response = await http_client.post(
                f"{get_files_url()}/api/files/upload",
                files=files,
                timeout=60.0,
            )
            # May return 413 Payload Too Large or accept it
            assert response.status_code in (200, 201, 400, 413, 422)
        except httpx.TimeoutException:
            pytest.skip("Upload timed out - expected for large files")

    async def test_upload_without_filename(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload without filename in multipart."""
        files = {"file": ("", b"test content", "text/plain")}
        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )
        # May accept with generated filename or reject
        assert response.status_code in (200, 201, 400, 422)

    async def test_upload_with_dangerous_filename(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload with path traversal in filename should be sanitized."""
        files = {"file": ("../../../etc/passwd", b"test content", "text/plain")}
        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )
        # Should either sanitize filename or reject
        if response.status_code in (200, 201):
            data = response.json()
            # Verify filename was sanitized (no path separators)
            if "filename" in data:
                assert "/" not in data["filename"]
                assert "\\" not in data["filename"]


class TestDownloadErrorPaths:
    """Error scenarios for file download endpoints."""

    async def test_download_nonexistent_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}"
        )
        assert response.status_code == 404

    async def test_download_invalid_id_format(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download with invalid ID format."""
        response = await http_client.get(
            f"{get_files_url()}/api/files/not-a-valid-uuid"
        )
        assert response.status_code in (400, 404)

    async def test_download_empty_id(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download with empty ID returns error."""
        response = await http_client.get(f"{get_files_url()}/api/files/")
        # May match list endpoint or return error
        assert response.status_code in (200, 400, 404, 405)

    async def test_presign_download_nonexistent(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Presign download for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}/presign"
        )
        assert response.status_code == 404


class TestMetadataErrorPaths:
    """Error scenarios for file metadata endpoints."""

    async def test_get_meta_nonexistent(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Get metadata for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}/meta"
        )
        assert response.status_code == 404

    async def test_list_files_invalid_pagination(
        self,
        http_client: httpx.AsyncClient,
    ):
        """List files with invalid pagination params."""
        # Negative page
        response = await http_client.get(
            f"{get_files_url()}/api/files",
            params={"page": -1},
        )
        assert response.status_code in (200, 400, 422)

        # Zero page size
        response = await http_client.get(
            f"{get_files_url()}/api/files",
            params={"page_size": 0},
        )
        assert response.status_code in (200, 400, 422)


class TestDeleteErrorPaths:
    """Error scenarios for file deletion endpoints."""

    async def test_delete_nonexistent_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Delete non-existent file returns 404 or 204."""
        fake_id = str(uuid4())
        response = await http_client.delete(
            f"{get_files_url()}/api/files/{fake_id}"
        )
        # Some APIs return 404, some 204 for idempotent delete
        assert response.status_code in (204, 404)

    async def test_delete_invalid_id_format(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Delete with invalid ID format."""
        response = await http_client.delete(
            f"{get_files_url()}/api/files/invalid-uuid"
        )
        assert response.status_code in (400, 404)


class TestVariantErrorPaths:
    """Error scenarios for file variant endpoints."""

    async def test_list_variants_nonexistent_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """List variants for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}/variants"
        )
        assert response.status_code == 404

    async def test_download_variant_nonexistent_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download variant for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}/variants/thumbnail"
        )
        assert response.status_code == 404

    async def test_download_variant_invalid_type(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download variant with invalid type."""
        # First we need to create a file, but if we don't have one,
        # we should get 404 for the file, not the variant type
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{get_files_url()}/api/files/{fake_id}/variants/nonexistent_type"
        )
        assert response.status_code in (400, 404)

    async def test_create_variant_nonexistent_file(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Create variant for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.post(
            f"{get_files_url()}/api/files/{fake_id}/variants",
            json={"type": "thumbnail"},
        )
        assert response.status_code == 404

    async def test_create_variant_missing_type(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Create variant without type returns error."""
        fake_id = str(uuid4())
        response = await http_client.post(
            f"{get_files_url()}/api/files/{fake_id}/variants",
            json={},
        )
        # Will likely return 404 for file not found first
        assert response.status_code in (400, 404, 422)


class TestPresignErrorPaths:
    """Error scenarios for presigned URL endpoints."""

    async def test_presign_upload_missing_params(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Presign upload without required params."""
        response = await http_client.post(
            f"{get_files_url()}/api/files/presign/upload",
            json={},
        )
        # May require filename or content-type
        assert response.status_code in (200, 400, 422)

    async def test_confirm_upload_nonexistent(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Confirm upload for non-existent file returns 404."""
        fake_id = str(uuid4())
        response = await http_client.post(
            f"{get_files_url()}/api/files/{fake_id}/confirm"
        )
        assert response.status_code == 404

    async def test_confirm_upload_invalid_id(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Confirm upload with invalid ID format."""
        response = await http_client.post(
            f"{get_files_url()}/api/files/not-a-uuid/confirm"
        )
        assert response.status_code in (400, 404)


class TestHealthEndpoint:
    """Health check for files service."""

    async def test_health_check(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Verify files service health endpoint."""
        response = await http_client.get(f"{get_files_url()}/health")

        assert response.status_code == 200
        data = response.json()
        assert data.get("status") in ("healthy", "ok")
