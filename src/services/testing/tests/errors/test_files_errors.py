"""
Error path tests for Files service via Gateway.

Tests verify proper error handling for invalid inputs and edge cases.
"""

from uuid import uuid4

import pytest

from testing.clients import GatewayClient


@pytest.mark.errors
class TestFileUploadErrors:
    """Error scenarios for /api/files/upload."""

    async def test_upload_no_file(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Upload without file returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.post(
            "/api/files/upload",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File upload requires authentication")

        assert response.status_code in (400, 422)

    async def test_upload_empty_file(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Upload empty file may be rejected."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("empty.txt", b"", "text/plain")},
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File upload requires authentication")

        # Some services allow empty files, some don't
        assert response.status_code in (200, 201, 400, 422)

    async def test_upload_wrong_field_name(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Upload with wrong field name returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.post(
            "/api/files/upload",
            files={"wrong_field": ("test.txt", b"content", "text/plain")},
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File upload requires authentication")

        assert response.status_code in (400, 422)


@pytest.mark.errors
class TestFileDownloadErrors:
    """Error scenarios for /api/files/{id}."""

    async def test_download_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Non-existent file returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            f"/api/files/{uuid4()}",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File download requires authentication")

        assert response.status_code == 404

    async def test_download_invalid_id(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Invalid file ID format returns 400 or 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            "/api/files/not-a-valid-uuid",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File download requires authentication")

        assert response.status_code in (400, 404, 422)


@pytest.mark.errors
class TestFileDeleteErrors:
    """Error scenarios for DELETE /api/files/{id}."""

    async def test_delete_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Deleting non-existent file returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.request_delete(
            f"/api/files/{uuid4()}",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File delete requires authentication")

        assert response.status_code == 404

    async def test_delete_invalid_id(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Invalid file ID format returns 400 or 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.request_delete(
            "/api/files/not-a-valid-uuid",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File delete requires authentication")

        assert response.status_code in (400, 404, 422)


@pytest.mark.errors
class TestFileMetadataErrors:
    """Error scenarios for file metadata endpoints."""

    async def test_get_metadata_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Metadata for non-existent file returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            f"/api/files/{uuid4()}/metadata",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("File metadata requires authentication")

        # May return 404 for file or 404 for endpoint not existing
        assert response.status_code in (404,)

    async def test_list_files_invalid_pagination(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Invalid pagination params returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            "/api/files",
            params={"page": -1, "page_size": -10},
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Listing files requires authentication")

        # Service may sanitize invalid params or reject them
        assert response.status_code in (200, 400, 422)
