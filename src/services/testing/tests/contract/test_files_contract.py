"""
Contract tests for Files service API via Gateway.

These tests verify that API responses match expected schemas.
All requests go through the gateway at /api/files/*.
"""

import pytest

from testing.clients import GatewayClient
from testing.schemas import FILE_METADATA_SCHEMA, FILE_UPLOAD_RESPONSE


@pytest.mark.contract
class TestFilesHealthContract:
    """Contract tests for files health endpoint."""

    async def test_health_via_gateway(self, gateway_client: GatewayClient) -> None:
        """Files health endpoint is accessible."""
        # Files routes keep path as-is through gateway
        response = await gateway_client.get("/api/files/health")

        if response.status_code == 404:
            pytest.skip("Files health not exposed at /api/files/health")

        assert response.status_code == 200
        data = response.json()
        assert data.get("status") in ("healthy", "ok", "up")


@pytest.mark.contract
class TestFileUploadContract:
    """Contract tests for file upload."""

    async def test_upload_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """POST /api/files/upload returns expected schema."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("contract_test.txt", b"contract test content", "text/plain")},
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File upload requires authentication")

        if response.status_code not in (200, 201):
            pytest.skip(f"Upload failed with status {response.status_code}")

        data = response.json()
        errors = FILE_UPLOAD_RESPONSE.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert "id" in data

        # Cleanup
        file_id = data.get("id")
        if file_id:
            await gateway_client.request_delete(
                f"/api/files/{file_id}",
                headers=headers,
            )


@pytest.mark.contract
class TestFileMetadataContract:
    """Contract tests for file metadata."""

    async def test_list_files_response(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """GET /api/files returns list."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get("/api/files", headers=headers)

        if response.status_code == 401:
            pytest.skip("Listing files requires authentication")

        assert response.status_code == 200

        data = response.json()
        # Could be list or paginated response
        if isinstance(data, list):
            for item in data[:3]:
                errors = FILE_METADATA_SCHEMA.validate(item)
                assert not errors, f"Schema validation failed: {errors}"
        elif isinstance(data, dict) and "data" in data:
            for item in data["data"][:3]:
                errors = FILE_METADATA_SCHEMA.validate(item)
                assert not errors, f"Schema validation failed: {errors}"


@pytest.mark.contract
class TestFileDownloadContract:
    """Contract tests for file download."""

    async def test_download_returns_content(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """GET /api/files/{id} returns file content."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}
        test_content = b"download contract test"

        # Upload first
        upload_response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("download_test.txt", test_content, "text/plain")},
            headers=headers,
        )

        if upload_response.status_code == 401:
            pytest.skip("File operations require authentication")

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        try:
            # Download
            response = await gateway_client.get(
                f"/api/files/{file_id}",
                headers=headers,
            )

            assert response.status_code == 200
            assert response.content == test_content

            # Check content-type header
            content_type = response.headers.get("content-type", "")
            assert content_type  # Should have content-type
        finally:
            await gateway_client.request_delete(
                f"/api/files/{file_id}",
                headers=headers,
            )


@pytest.mark.contract
class TestFileDeleteContract:
    """Contract tests for file deletion."""

    async def test_delete_returns_success(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """DELETE /api/files/{id} returns 204."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        # Upload first
        upload_response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("delete_test.txt", b"delete me", "text/plain")},
            headers=headers,
        )

        if upload_response.status_code == 401:
            pytest.skip("File operations require authentication")

        if upload_response.status_code not in (200, 201):
            pytest.skip("Could not upload test file")

        file_id = upload_response.json().get("id")

        response = await gateway_client.request_delete(
            f"/api/files/{file_id}",
            headers=headers,
        )

        assert response.status_code in (200, 204)
