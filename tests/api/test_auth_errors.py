"""
Error path tests for the Auth service.

These tests verify proper error handling for edge cases and invalid inputs.
"""

from datetime import timedelta
from typing import Callable

import httpx
import pytest

from fixtures.auth import AuthSession, AuthUser


class TestValidateErrorPaths:
    """Error scenarios for the /auth/validate endpoint."""

    async def test_validate_with_malformed_bearer_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Malformed bearer token returns 401."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": "Bearer not.a.valid.jwt.token"},
        )

        assert response.status_code == 401
        assert response.json()["valid"] is False

    async def test_validate_with_empty_bearer_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Empty bearer token returns 401."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": "Bearer "},
        )

        assert response.status_code == 401

    async def test_validate_with_wrong_algorithm_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Token signed with wrong algorithm returns 401."""
        # HS256 token instead of RS256
        fake_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U"
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Bearer {fake_token}"},
        )

        assert response.status_code == 401
        assert response.json()["valid"] is False

    async def test_validate_with_expired_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_access_token: Callable[..., str],
    ):
        """Expired access token returns 401."""
        expired_token = create_access_token(
            user_id=test_user.id,
            email=test_user.email,
            username=test_user.username,
            expires_in=timedelta(seconds=-1),
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Bearer {expired_token}"},
        )

        assert response.status_code == 401
        assert response.json()["valid"] is False

    async def test_validate_with_future_nbf_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_access_token: Callable[..., str],
    ):
        """Token with future not-before claim should be rejected."""
        from datetime import datetime, timezone

        future_nbf = datetime.now(timezone.utc) + timedelta(hours=1)
        token = create_access_token(
            user_id=test_user.id,
            email=test_user.email,
            username=test_user.username,
            nbf=future_nbf,
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Bearer {token}"},
        )

        # Should be rejected since token is not yet valid
        assert response.status_code == 401

    async def test_validate_with_missing_authorization_header(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Missing authorization header returns 401."""
        response = await http_client.post(f"{base_urls['auth']}/auth/validate")

        assert response.status_code == 401
        assert response.json()["valid"] is False

    async def test_validate_with_basic_auth_instead_of_bearer(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Basic auth instead of Bearer returns 401."""
        import base64

        credentials = base64.b64encode(b"user:pass").decode()
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Basic {credentials}"},
        )

        assert response.status_code == 401


class TestRefreshErrorPaths:
    """Error scenarios for the /auth/refresh endpoint."""

    async def test_refresh_with_missing_body(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Missing request body returns error."""
        response = await http_client.post(f"{base_urls['auth']}/auth/refresh")

        assert response.status_code in (400, 401, 422)

    async def test_refresh_with_empty_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Empty refresh token returns 401."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": ""},
        )

        assert response.status_code == 401

    async def test_refresh_with_access_token_instead_of_refresh(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_access_token: Callable[..., str],
    ):
        """Using access token as refresh token should fail."""
        access_token = create_access_token(
            user_id=test_user.id,
            email=test_user.email,
            username=test_user.username,
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": access_token},
        )

        # Access tokens should not work as refresh tokens
        assert response.status_code == 401

    async def test_refresh_with_malformed_json(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Malformed JSON returns 400."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            content=b"not valid json",
            headers={"Content-Type": "application/json"},
        )

        assert response.status_code in (400, 422)


class TestMeErrorPaths:
    """Error scenarios for the /auth/me endpoint."""

    async def test_me_with_expired_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        redis_client,
    ):
        """Expired session returns 401."""
        from fixtures.auth import create_test_session_in_redis, delete_test_session_from_redis

        # Create an already-expired session
        session = await create_test_session_in_redis(
            redis_client,
            test_user.id,
            expires_in=timedelta(seconds=-1),
        )

        try:
            response = await http_client.get(
                f"{base_urls['auth']}/auth/me",
                headers={"X-Session-Token": session.token},
            )

            assert response.status_code == 401
        finally:
            await delete_test_session_from_redis(redis_client, session)

    async def test_me_with_deleted_user_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        redis_client,
    ):
        """Session for non-existent user returns 401."""
        from fixtures.auth import create_test_session_in_redis, delete_test_session_from_redis
        from uuid import uuid4

        # Create session for a user that doesn't exist
        fake_user_id = str(uuid4())
        session = await create_test_session_in_redis(redis_client, fake_user_id)

        try:
            response = await http_client.get(
                f"{base_urls['auth']}/auth/me",
                headers={"X-Session-Token": session.token},
            )

            # Should fail because user doesn't exist
            assert response.status_code in (401, 404)
        finally:
            await delete_test_session_from_redis(redis_client, session)

    async def test_me_with_tampered_session_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: AuthSession,
    ):
        """Tampered session token returns 401."""
        # Modify the token slightly
        tampered_token = test_session.token[:-4] + "xxxx"

        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            headers={"X-Session-Token": tampered_token},
        )

        assert response.status_code == 401


class TestLogoutErrorPaths:
    """Error scenarios for logout functionality."""

    async def test_logout_without_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Logout without session returns appropriate error."""
        response = await http_client.post(f"{base_urls['auth']}/auth/logout")

        # Should either return 401 or succeed silently (idempotent)
        assert response.status_code in (200, 204, 401)

    async def test_logout_with_invalid_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Logout with invalid session token."""
        response = await http_client.post(
            f"{base_urls['auth']}/auth/logout",
            headers={"X-Session-Token": "invalid_token"},
        )

        # Should either return 401 or succeed silently (idempotent)
        assert response.status_code in (200, 204, 401)


class TestRateLimiting:
    """Rate limiting behavior tests."""

    @pytest.mark.slow
    async def test_rapid_validate_requests(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Rapid requests should eventually be rate limited."""
        import asyncio

        responses = []
        for _ in range(50):
            response = await http_client.post(
                f"{base_urls['auth']}/auth/validate",
                headers={"Authorization": "Bearer invalid"},
            )
            responses.append(response.status_code)
            await asyncio.sleep(0.01)  # Small delay

        # Should have mostly 401s, possibly some 429s if rate limited
        status_counts = {status: responses.count(status) for status in set(responses)}
        assert 401 in status_counts or 429 in status_counts
