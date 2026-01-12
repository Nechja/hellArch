"""
Error path tests for Auth service via Gateway.

Tests verify proper error handling for invalid inputs and edge cases.
"""

import pytest

from testing.clients import GatewayClient


@pytest.mark.errors
class TestAuthValidateErrors:
    """Error scenarios for /api/auth/validate."""

    async def test_validate_malformed_bearer_token(
        self, gateway_client: GatewayClient
    ) -> None:
        """Malformed bearer token returns 401."""
        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": "Bearer not.a.valid.jwt"},
        )
        assert response.status_code == 401
        assert response.json().get("valid") is False

    async def test_validate_empty_bearer_token(
        self, gateway_client: GatewayClient
    ) -> None:
        """Empty bearer token returns 401."""
        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": "Bearer "},
        )
        assert response.status_code == 401

    async def test_validate_wrong_algorithm_token(
        self, gateway_client: GatewayClient
    ) -> None:
        """HS256 token instead of RS256 returns 401."""
        # This is an HS256-signed token
        fake_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U"
        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": f"Bearer {fake_token}"},
        )
        assert response.status_code == 401

    async def test_validate_basic_auth_instead_of_bearer(
        self, gateway_client: GatewayClient
    ) -> None:
        """Basic auth instead of Bearer returns 401."""
        import base64
        credentials = base64.b64encode(b"user:pass").decode()
        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": f"Basic {credentials}"},
        )
        assert response.status_code == 401


@pytest.mark.errors
class TestAuthRefreshErrors:
    """Error scenarios for /api/auth/refresh."""

    async def test_refresh_missing_body(self, gateway_client: GatewayClient) -> None:
        """Missing request body returns error."""
        response = await gateway_client.post("/api/auth/refresh")
        assert response.status_code in (400, 401, 422)

    async def test_refresh_empty_token(self, gateway_client: GatewayClient) -> None:
        """Empty refresh token returns 401."""
        response = await gateway_client.post(
            "/api/auth/refresh",
            json={"refresh_token": ""},
        )
        assert response.status_code == 401

    async def test_refresh_invalid_token(self, gateway_client: GatewayClient) -> None:
        """Invalid refresh token returns 401."""
        response = await gateway_client.post(
            "/api/auth/refresh",
            json={"refresh_token": "invalid_token"},
        )
        assert response.status_code == 401

    async def test_refresh_malformed_json(self, gateway_client: GatewayClient) -> None:
        """Malformed JSON returns 400."""
        response = await gateway_client.post(
            "/api/auth/refresh",
            content=b"not valid json",
            headers={"Content-Type": "application/json"},
        )
        assert response.status_code in (400, 422)


@pytest.mark.errors
class TestAuthMeErrors:
    """Error scenarios for /api/auth/me."""

    async def test_me_without_session(self, gateway_client: GatewayClient) -> None:
        """Request without session returns 401."""
        response = await gateway_client.get("/api/auth/me")
        assert response.status_code == 401

    async def test_me_invalid_session(self, gateway_client: GatewayClient) -> None:
        """Invalid session token returns 401."""
        response = await gateway_client.get(
            "/api/auth/me",
            headers={"X-Session-Token": "invalid_token"},
        )
        assert response.status_code == 401

    async def test_me_invalid_bearer(self, gateway_client: GatewayClient) -> None:
        """Invalid bearer token returns 401."""
        response = await gateway_client.get(
            "/api/auth/me",
            headers={"Authorization": "Bearer invalid"},
        )
        assert response.status_code == 401
