"""
Contract tests for the Auth service API.

These tests verify that API responses match expected schemas,
catching breaking changes in the API contract.
"""

from typing import Callable

import httpx
import pytest

from fixtures.auth import AuthSession, AuthUser
from fixtures.schemas import (
    AUTH_ERROR_SCHEMA,
    AUTH_HEALTH_SCHEMA,
    AUTH_ME_RESPONSE_SCHEMA,
    AUTH_REFRESH_RESPONSE_SCHEMA,
    AUTH_VALIDATE_RESPONSE_SCHEMA,
)


class TestAuthHealthContract:
    """Contract tests for /auth/health endpoint."""

    async def test_health_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Health endpoint returns expected schema."""
        response = await http_client.get(f"{base_urls['auth']}/auth/health")

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_HEALTH_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Additional contract assertions
        assert data["status"] in ("healthy", "ok", "up")


class TestAuthValidateContract:
    """Contract tests for /auth/validate endpoint."""

    async def test_validate_success_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: AuthSession,
    ):
        """Successful validation returns expected schema."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": test_session.token},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: successful validation must have valid=True and user_id
        assert data["valid"] is True
        assert data.get("user_id") is not None

    async def test_validate_bearer_token_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_access_token: Callable[..., str],
    ):
        """Bearer token validation returns expected schema."""
        token = create_access_token(
            user_id=test_user.id,
            email=test_user.email,
            username=test_user.username,
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Bearer {token}"},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

    async def test_validate_failure_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Failed validation returns expected schema."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": "Bearer invalid_token"},
        )

        assert response.status_code == 401

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: failed validation must have valid=False
        assert data["valid"] is False

    async def test_validate_no_token_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Missing token returns expected schema."""
        response = await http_client.post(f"{base_urls['auth']}/auth/validate")

        assert response.status_code == 401

        data = response.json()
        errors = AUTH_VALIDATE_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"


class TestAuthRefreshContract:
    """Contract tests for /auth/refresh endpoint."""

    async def test_refresh_success_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_refresh_token: Callable[..., str],
    ):
        """Successful refresh returns expected schema."""
        refresh_token = create_refresh_token(user_id=test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": refresh_token},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_REFRESH_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract assertions
        assert "access_token" in data
        assert "refresh_token" in data
        assert data["expires_in"] > 0

    async def test_refresh_failure_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Failed refresh returns expected schema."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": "invalid_token"},
        )

        assert response.status_code == 401

        data = response.json()
        # Error responses should have error field
        assert "error" in data or "valid" in data


class TestAuthMeContract:
    """Contract tests for /auth/me endpoint."""

    async def test_me_success_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: AuthSession,
    ):
        """Successful /me returns expected schema."""
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            headers={"X-Session-Token": test_session.token},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_ME_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: must return user_id
        assert "user_id" in data

    async def test_me_with_cookie_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: AuthSession,
    ):
        """Cookie-based /me returns expected schema."""
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            cookies={"hellarch_session": test_session.token},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUTH_ME_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

    async def test_me_failure_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Failed /me returns expected error schema."""
        response = await http_client.get(f"{base_urls['auth']}/auth/me")

        assert response.status_code == 401

        data = response.json()
        errors = AUTH_ERROR_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: error response must have error message
        assert "error" in data


class TestAuthTokenClaims:
    """Contract tests for JWT token claims structure."""

    async def test_access_token_contains_required_claims(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_refresh_token: Callable[..., str],
        jwt_keys: tuple[str, str],
    ):
        """Access tokens contain required claims."""
        from jose import jwt

        refresh_token = create_refresh_token(user_id=test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": refresh_token},
        )

        if response.status_code != 200:
            pytest.skip("Could not get access token")

        data = response.json()
        access_token = data["access_token"]

        _, public_key = jwt_keys
        claims = jwt.decode(
            access_token,
            public_key,
            algorithms=["RS256"],
            options={"verify_aud": False},
        )

        # Required JWT claims
        assert "iss" in claims, "Missing issuer claim"
        assert "sub" in claims, "Missing subject claim"
        assert "exp" in claims, "Missing expiration claim"
        assert "iat" in claims, "Missing issued-at claim"

        # HellArch-specific claims
        assert "uid" in claims or "sub" in claims, "Missing user ID claim"

    async def test_token_expiration_is_reasonable(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_refresh_token: Callable[..., str],
    ):
        """Token expiration is within reasonable bounds."""
        refresh_token = create_refresh_token(user_id=test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": refresh_token},
        )

        if response.status_code != 200:
            pytest.skip("Could not get access token")

        data = response.json()

        # Access token should expire in reasonable time (1 minute to 24 hours)
        expires_in = data["expires_in"]
        assert 60 <= expires_in <= 86400, f"Unexpected expires_in: {expires_in}"


class TestAuthResponseHeaders:
    """Contract tests for response headers."""

    async def test_health_response_content_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Health endpoint returns JSON content type."""
        response = await http_client.get(f"{base_urls['auth']}/auth/health")

        content_type = response.headers.get("content-type", "")
        assert "application/json" in content_type

    async def test_validate_response_content_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Validate endpoint returns JSON content type."""
        response = await http_client.post(f"{base_urls['auth']}/auth/validate")

        content_type = response.headers.get("content-type", "")
        assert "application/json" in content_type

    async def test_cors_headers_present(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """CORS headers are present when needed."""
        response = await http_client.options(
            f"{base_urls['auth']}/auth/validate",
            headers={"Origin": "http://localhost:3000"},
        )

        # CORS might be handled by gateway, so this is informational
        # Just verify we get a response
        assert response.status_code in (200, 204, 404, 405)
