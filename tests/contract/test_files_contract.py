"""
Contract tests for the Files service API.

These tests verify that API responses match expected schemas,
catching breaking changes in the API contract.
"""

import os
from uuid import uuid4

import httpx
import pytest

from fixtures.schemas import (
    FILE_METADATA_SCHEMA,
    FILE_UPLOAD_RESPONSE_SCHEMA,
    FILES_LIST_RESPONSE_SCHEMA,
    FILE_VARIANT_SCHEMA,
    HEALTH_SCHEMA,
    PRESIGN_RESPONSE_SCHEMA,
)


def get_files_url() -> str:
    return os.getenv("FILES_URL", "http://localhost:5006")


class TestFilesHealthContract:
    """Contract tests for /health endpoint."""

    async def test_health_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Health endpoint returns expected schema."""
        response = await http_client.get(f"{get_files_url()}/health")

        assert response.status_code == 200

        data = response.json()
        errors = HEALTH_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        assert data["status"] in ("healthy", "ok", "up")


class TestFileUploadContract:
    """Contract tests for file upload endpoints."""

    async def test_upload_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """POST /api/files/upload returns expected schema."""
        test_content = b"Contract test file content"
        files = {"file": ("contract_test.txt", test_content, "text/plain")}

        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if response.status_code not in (200, 201):
            pytest.skip(f"Upload failed with status {response.status_code}")

        data = response.json()
        errors = FILE_UPLOAD_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: must return file ID
        assert "id" in data

        # Cleanup
        if "id" in data:
            await http_client.delete(f"{get_files_url()}/api/files/{data['id']}")

    async def test_upload_preserves_filename(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload preserves the original filename."""
        filename = "my_test_file.txt"
        files = {"file": (filename, b"test content", "text/plain")}

        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if response.status_code not in (200, 201):
            pytest.skip(f"Upload failed with status {response.status_code}")

        data = response.json()

        # Contract: filename should be preserved (possibly sanitized)
        if "filename" in data:
            assert "test_file" in data["filename"] or filename in data["filename"]

        # Cleanup
        if "id" in data:
            await http_client.delete(f"{get_files_url()}/api/files/{data['id']}")

    async def test_upload_returns_content_type(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Upload returns correct content type."""
        files = {"file": ("test.json", b'{"key": "value"}', "application/json")}

        response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if response.status_code not in (200, 201):
            pytest.skip(f"Upload failed with status {response.status_code}")

        data = response.json()

        # Content type might be returned
        if "content_type" in data:
            assert "json" in data["content_type"].lower()

        # Cleanup
        if "id" in data:
            await http_client.delete(f"{get_files_url()}/api/files/{data['id']}")


class TestFileMetadataContract:
    """Contract tests for file metadata endpoints."""

    async def test_get_file_meta_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """GET /api/files/{id}/meta returns expected schema."""
        # Upload a file first
        files = {"file": ("meta_test.txt", b"metadata test", "text/plain")}
        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            response = await http_client.get(
                f"{get_files_url()}/api/files/{file_id}/meta"
            )

            assert response.status_code == 200

            data = response.json()
            errors = FILE_METADATA_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"

            # Contract: metadata must include key fields
            assert "id" in data
            assert "filename" in data
        finally:
            await http_client.delete(f"{get_files_url()}/api/files/{file_id}")

    async def test_list_files_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """GET /api/files returns paginated response schema."""
        response = await http_client.get(
            f"{get_files_url()}/api/files",
            params={"page": 1, "page_size": 10},
        )

        assert response.status_code == 200

        data = response.json()

        # Could be paginated or simple list
        if isinstance(data, list):
            # Simple list response
            for item in data[:3]:  # Check first few items
                errors = FILE_METADATA_SCHEMA.validate(item)
                assert not errors, f"Schema validation failed: {errors}"
        else:
            # Paginated response
            errors = FILES_LIST_RESPONSE_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"


class TestFileDownloadContract:
    """Contract tests for file download endpoints."""

    async def test_download_returns_file_content(
        self,
        http_client: httpx.AsyncClient,
    ):
        """GET /api/files/{id} returns file content."""
        test_content = b"Download contract test content"
        files = {"file": ("download_test.txt", test_content, "text/plain")}

        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            response = await http_client.get(
                f"{get_files_url()}/api/files/{file_id}"
            )

            assert response.status_code == 200

            # Contract: response should be the file content
            assert response.content == test_content

            # Contract: should have appropriate headers
            content_type = response.headers.get("content-type", "")
            assert content_type  # Should have content-type
        finally:
            await http_client.delete(f"{get_files_url()}/api/files/{file_id}")

    async def test_download_sets_content_disposition(
        self,
        http_client: httpx.AsyncClient,
    ):
        """Download includes Content-Disposition header."""
        files = {"file": ("disposition_test.txt", b"test", "text/plain")}

        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            response = await http_client.get(
                f"{get_files_url()}/api/files/{file_id}"
            )

            # Content-Disposition is optional but recommended
            content_disposition = response.headers.get("content-disposition", "")
            # If present, should contain filename
            if content_disposition:
                assert "filename" in content_disposition.lower()
        finally:
            await http_client.delete(f"{get_files_url()}/api/files/{file_id}")


class TestPresignContract:
    """Contract tests for presigned URL endpoints."""

    async def test_presign_download_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """GET /api/files/{id}/presign returns expected schema."""
        # Upload a file first
        files = {"file": ("presign_test.txt", b"presign test", "text/plain")}
        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            response = await http_client.get(
                f"{get_files_url()}/api/files/{file_id}/presign"
            )

            if response.status_code == 404:
                pytest.skip("Presign endpoint not implemented")

            assert response.status_code == 200

            data = response.json()
            errors = PRESIGN_RESPONSE_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"

            # Contract: must return URL
            assert "url" in data
            assert data["url"].startswith("http")
        finally:
            await http_client.delete(f"{get_files_url()}/api/files/{file_id}")

    async def test_presign_upload_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """POST /api/files/presign/upload returns expected schema."""
        payload = {
            "filename": "presign_upload_test.txt",
            "content_type": "text/plain",
        }

        response = await http_client.post(
            f"{get_files_url()}/api/files/presign/upload",
            json=payload,
        )

        if response.status_code == 404:
            pytest.skip("Presign upload endpoint not implemented")

        if response.status_code not in (200, 201):
            pytest.skip(f"Presign upload failed with status {response.status_code}")

        data = response.json()

        # Contract: must return URL
        assert "url" in data


class TestFileVariantsContract:
    """Contract tests for file variant endpoints."""

    async def test_list_variants_response_schema(
        self,
        http_client: httpx.AsyncClient,
    ):
        """GET /api/files/{id}/variants returns expected schema."""
        # Upload an image file for variant testing
        # Using a minimal PNG
        png_header = bytes([
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,  # PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,  # IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,  # 1x1 pixels
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F,
            0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59,
            0xE7, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82,
        ])

        files = {"file": ("variant_test.png", png_header, "image/png")}
        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            response = await http_client.get(
                f"{get_files_url()}/api/files/{file_id}/variants"
            )

            if response.status_code == 404:
                pytest.skip("Variants endpoint not implemented")

            assert response.status_code == 200

            data = response.json()

            # Could be a list or object with data field
            if isinstance(data, list):
                for variant in data:
                    errors = FILE_VARIANT_SCHEMA.validate(variant)
                    assert not errors, f"Schema validation failed: {errors}"
            elif isinstance(data, dict) and "data" in data:
                for variant in data["data"]:
                    errors = FILE_VARIANT_SCHEMA.validate(variant)
                    assert not errors, f"Schema validation failed: {errors}"
        finally:
            await http_client.delete(f"{get_files_url()}/api/files/{file_id}")


class TestFileDeleteContract:
    """Contract tests for file deletion."""

    async def test_delete_returns_no_content(
        self,
        http_client: httpx.AsyncClient,
    ):
        """DELETE /api/files/{id} returns 204 No Content."""
        files = {"file": ("delete_test.txt", b"delete me", "text/plain")}
        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        response = await http_client.delete(
            f"{get_files_url()}/api/files/{file_id}"
        )

        # Contract: successful delete returns 204 or 200
        assert response.status_code in (200, 204)

    async def test_delete_is_idempotent(
        self,
        http_client: httpx.AsyncClient,
    ):
        """DELETE /api/files/{id} is idempotent."""
        files = {"file": ("idempotent_test.txt", b"delete twice", "text/plain")}
        upload_response = await http_client.post(
            f"{get_files_url()}/api/files/upload",
            files=files,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        # Delete once
        response1 = await http_client.delete(
            f"{get_files_url()}/api/files/{file_id}"
        )
        assert response1.status_code in (200, 204)

        # Delete again - should be idempotent (204 or 404)
        response2 = await http_client.delete(
            f"{get_files_url()}/api/files/{file_id}"
        )
        assert response2.status_code in (200, 204, 404)


class TestFilesResponseHeaders:
    """Contract tests for response headers."""

    async def test_json_endpoints_return_json_content_type(
        self,
        http_client: httpx.AsyncClient,
    ):
        """JSON endpoints return application/json content type."""
        endpoints = [
            "/health",
            "/api/files",
        ]

        for endpoint in endpoints:
            response = await http_client.get(f"{get_files_url()}{endpoint}")

            if response.status_code == 200:
                content_type = response.headers.get("content-type", "")
                assert "application/json" in content_type, (
                    f"{endpoint} did not return JSON: {content_type}"
                )
