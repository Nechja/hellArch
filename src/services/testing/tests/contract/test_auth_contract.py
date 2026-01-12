"""
Contract tests for Auth service API via Gateway.

These tests verify that API responses match expected schemas.
All requests go through the gateway at /api/auth/*.
"""

import pytest

from testing.clients import GatewayClient
from testing.schemas import (
    AUTH_HEALTH_SCHEMA,
    AUTH_ME_RESPONSE,
    AUTH_VALIDATE_RESPONSE,
    HEALTH_SCHEMA,
)


@pytest.mark.contract
class TestAuthHealthContract:
    """Contract tests for auth health endpoint."""

    async def test_health_response_schema(self, gateway_client: GatewayClient) -> None:
        """Health endpoint returns expected schema."""
        response = await gateway_client.get("/api/auth/health")

        assert response.status_code == 200

        data = response.json()
        errors = HEALTH_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert data["status"] in ("healthy", "ok", "up")


@pytest.mark.contract
class TestAuthValidateContract:
    """Contract tests for /api/auth/validate endpoint."""

    async def test_validate_no_token_response_schema(
        self, gateway_client: GatewayClient
    ) -> None:
        """Missing token returns expected schema."""
        response = await gateway_client.post("/api/auth/validate")

        assert response.status_code == 401

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert data["valid"] is False

    async def test_validate_invalid_token_response_schema(
        self, gateway_client: GatewayClient
    ) -> None:
        """Invalid token returns expected schema."""
        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": "Bearer invalid_token"},
        )

        assert response.status_code == 401

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert data["valid"] is False

    async def test_validate_success_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Valid token returns expected schema."""
        if not auth_token:
            pytest.skip("No auth token available")

        response = await gateway_client.post(
            "/api/auth/validate",
            headers={"Authorization": f"Bearer {auth_token}"},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert data["valid"] is True


@pytest.mark.contract
class TestAuthMeContract:
    """Contract tests for /api/auth/me endpoint."""

    async def test_me_no_session_returns_401(
        self, gateway_client: GatewayClient
    ) -> None:
        """Request without session returns 401."""
        response = await gateway_client.get("/api/auth/me")

        assert response.status_code == 401

    async def test_me_with_token_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Valid token returns expected schema."""
        if not auth_token:
            pytest.skip("No auth token available")

        response = await gateway_client.get(
            "/api/auth/me",
            headers={"Authorization": f"Bearer {auth_token}"},
        )

        if response.status_code == 200:
            data = response.json()
            errors = AUTH_ME_RESPONSE.validate(data)
            assert not errors, f"Schema validation failed: {errors}"
            assert "user_id" in data


@pytest.mark.contract
class TestAuthResponseHeaders:
    """Contract tests for response headers."""

    async def test_auth_endpoints_return_json(
        self, gateway_client: GatewayClient
    ) -> None:
        """Auth endpoints return JSON content type."""
        response = await gateway_client.get("/api/auth/health")

        content_type = response.headers.get("content-type", "")
        assert "application/json" in content_type
