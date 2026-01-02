from uuid import uuid4

import httpx
import pytest


class TestHealthEndpoint:
    async def test_health_returns_healthy(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(f"{base_urls['user']}/user/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["database"] is True


class TestGetProfileEndpoint:
    async def test_get_profile_not_found(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        user_id = uuid4()
        response = await http_client.get(f"{base_urls['user']}/user/{user_id}")

        assert response.status_code == 404
        data = response.json()
        assert data["error"] == "not_found"


class TestCreateProfileEndpoint:
    async def test_create_profile(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        user_id = str(uuid4())
        response = await http_client.post(
            f"{base_urls['user']}/user",
            json={
                "user_id": user_id,
                "email": "newuser@example.com",
                "display_name": "New User",
                "avatar_url": "https://example.com/avatar.png",
            },
        )

        assert response.status_code == 201
        data = response.json()
        assert data["auth_user_id"] == user_id
        assert data["email"] == "newuser@example.com"
        assert data["display_name"] == "New User"

    async def test_create_profile_invalid_body(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.post(
            f"{base_urls['user']}/user",
            content="invalid json",
            headers={"Content-Type": "application/json"},
        )

        assert response.status_code == 400


class TestUpdateProfileEndpoint:
    async def test_update_profile_not_found(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        user_id = uuid4()
        response = await http_client.put(
            f"{base_urls['user']}/user/{user_id}",
            json={
                "display_name": "Updated Name",
            },
        )

        assert response.status_code == 404


class TestGetPreferencesEndpoint:
    async def test_get_preferences_not_found(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        user_id = uuid4()
        response = await http_client.get(f"{base_urls['user']}/user/{user_id}/preferences")

        assert response.status_code == 404


class TestMeEndpoint:
    async def test_me_without_user_id_header(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(f"{base_urls['user']}/user/me")

        assert response.status_code == 401
        assert response.json()["error"] == "unauthorized"

    async def test_me_with_invalid_user_id(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(
            f"{base_urls['user']}/user/me",
            headers={"X-User-ID": "not-a-uuid"},
        )

        assert response.status_code == 400
        assert response.json()["error"] == "bad_request"
