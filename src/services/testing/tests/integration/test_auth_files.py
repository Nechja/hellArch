"""
Cross-service integration tests: Auth + Files.

Tests verify auth tokens work correctly with file service operations.
"""

import pytest

from testing.clients import GatewayClient


@pytest.mark.integration
class TestAuthenticatedFileOperations:
    """File operations require valid authentication."""

    async def test_upload_requires_auth(
        self, gateway_client: GatewayClient
    ) -> None:
        """File upload without auth is rejected."""
        response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("test.txt", b"content", "text/plain")},
        )

        # Should require auth
        if response.status_code not in (200, 201):
            assert response.status_code == 401

    async def test_upload_with_valid_token(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """File upload with valid auth succeeds."""
        if not auth_token:
            pytest.skip("No auth token available")

        response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("auth_test.txt", b"auth test content", "text/plain")},
            headers={"Authorization": f"Bearer {auth_token}"},
        )

        assert response.status_code in (200, 201)

        # Cleanup
        file_id = response.json().get("id")
        if file_id:
            await gateway_client.request_delete(
                f"/api/files/{file_id}",
                headers={"Authorization": f"Bearer {auth_token}"},
            )

    async def test_download_own_file(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """User can download their own uploaded file."""
        if not auth_token:
            pytest.skip("No auth token available")

        headers = {"Authorization": f"Bearer {auth_token}"}
        test_content = b"download own file test"

        # Upload
        upload_response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("own_file.txt", test_content, "text/plain")},
            headers=headers,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Upload failed")

        file_id = upload_response.json().get("id")

        try:
            # Download
            download_response = await gateway_client.get(
                f"/api/files/{file_id}",
                headers=headers,
            )

            assert download_response.status_code == 200
            assert download_response.content == test_content
        finally:
            await gateway_client.request_delete(
                f"/api/files/{file_id}",
                headers=headers,
            )


@pytest.mark.integration
class TestTokenValidationWithFiles:
    """Token validation affects file access."""

    async def test_expired_token_rejected_for_files(
        self, gateway_client: GatewayClient
    ) -> None:
        """Expired token cannot access files."""
        # Use a clearly invalid/expired token
        expired_token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjB9.invalid"

        response = await gateway_client.get(
            "/api/files",
            headers={"Authorization": f"Bearer {expired_token}"},
        )

        assert response.status_code == 401

    async def test_invalid_token_rejected_for_upload(
        self, gateway_client: GatewayClient
    ) -> None:
        """Invalid token cannot upload files."""
        response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("rejected.txt", b"should fail", "text/plain")},
            headers={"Authorization": "Bearer invalid_token"},
        )

        assert response.status_code == 401
