from datetime import timedelta
from typing import Callable

import httpx
import pytest

from fixtures.auth import TestSession, TestUser


class TestHealthEndpoint:
    async def test_health_returns_healthy(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(f"{base_urls['auth']}/auth/health")

        assert response.status_code == 200
        assert response.json() == {"status": "healthy"}


class TestValidateEndpoint:
    async def test_validate_with_valid_session_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: TestSession,
    ):
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": test_session.token},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["valid"] is True
        assert data["user_id"] == test_session.user_id

    async def test_validate_with_valid_bearer_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: TestUser,
        create_access_token: Callable[..., str],
    ):
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
        assert data["valid"] is True
        assert data["user_id"] == test_user.id

    async def test_validate_with_invalid_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": "Bearer invalid_token"},
        )

        assert response.status_code == 401
        assert response.json()["valid"] is False

    async def test_validate_with_no_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.post(f"{base_urls['auth']}/auth/validate")

        assert response.status_code == 401
        assert response.json()["valid"] is False


class TestRefreshEndpoint:
    async def test_refresh_with_valid_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: TestUser,
        create_refresh_token: Callable[..., str],
    ):
        refresh_token = create_refresh_token(user_id=test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": refresh_token},
        )

        assert response.status_code == 200
        data = response.json()
        assert "access_token" in data
        assert "refresh_token" in data
        assert "expires_in" in data

    async def test_refresh_with_invalid_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": "invalid_token"},
        )

        assert response.status_code == 401

    async def test_refresh_with_expired_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: TestUser,
        create_refresh_token: Callable[..., str],
    ):
        expired_token = create_refresh_token(
            user_id=test_user.id,
            expires_in=timedelta(seconds=-1),
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": expired_token},
        )

        assert response.status_code == 401


class TestMeEndpoint:
    async def test_me_with_valid_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: TestSession,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            headers={"X-Session-Token": test_session.token},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["user_id"] == test_session.user_id

    async def test_me_with_session_cookie(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: TestSession,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            cookies={"hellarch_session": test_session.token},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["user_id"] == test_session.user_id

    async def test_me_without_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(f"{base_urls['auth']}/auth/me")

        assert response.status_code == 401
        assert response.json()["error"] == "no session token"

    async def test_me_with_invalid_session(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            headers={"X-Session-Token": "invalid_token"},
        )

        assert response.status_code == 401
        assert response.json()["error"] == "invalid session"
