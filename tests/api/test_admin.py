from typing import Callable

import httpx

from fixtures.auth import AuthUser


class TestAdminUserEndpoints:
    async def test_get_user_includes_permissions_and_active(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}",
        )

        assert response.status_code == 200
        data = response.json()
        assert data["id"] == test_user.id
        assert "is_active" in data
        assert data["is_active"] is True
        assert "permissions" in data


class TestPermissionManagement:
    async def test_get_permissions(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
        )

        assert response.status_code == 200
        data = response.json()
        assert "permissions" in data
        assert isinstance(data["permissions"], dict)

    async def test_set_permissions(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        permissions = {"admin.access": True, "user.read": True, "user.write": False}

        response = await http_client.put(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
            json={"permissions": permissions},
        )

        assert response.status_code == 200

        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
        )
        assert get_response.status_code == 200
        data = get_response.json()
        assert data["permissions"]["admin.access"] is True
        assert data["permissions"]["user.read"] is True

    async def test_grant_permission(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        response = await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions/grant",
            json={"permission": "admin.users.write"},
        )

        assert response.status_code == 204

        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
        )
        assert get_response.status_code == 200
        data = get_response.json()
        assert data["permissions"].get("admin.users.write") is True

    async def test_revoke_permission(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions/grant",
            json={"permission": "temp.permission"},
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions/revoke",
            json={"permission": "temp.permission"},
        )

        assert response.status_code == 204

        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
        )
        assert get_response.status_code == 200
        data = get_response.json()
        assert "temp.permission" not in data["permissions"]

    async def test_get_permissions_user_not_found(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        fake_id = "00000000-0000-0000-0000-000000000000"
        response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{fake_id}/permissions",
        )

        assert response.status_code == 404


class TestUserActivation:
    async def test_deactivate_user(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        response = await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        assert response.status_code == 204

        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}",
        )
        assert get_response.status_code == 200
        assert get_response.json()["is_active"] is False

    async def test_activate_user(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/activate",
        )

        assert response.status_code == 204

        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}",
        )
        assert get_response.status_code == 200
        assert get_response.json()["is_active"] is True

    async def test_deactivate_invalidates_sessions(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        test_session_factory: Callable,
    ):
        session = await test_session_factory(test_user.id)

        validate_response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )
        assert validate_response.status_code == 200

        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        validate_response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )
        assert validate_response.status_code == 401


class TestDeactivatedUserAuthFlow:
    async def test_deactivated_user_session_validation_fails(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        test_session_factory: Callable,
    ):
        session = await test_session_factory(test_user.id)

        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )

        assert response.status_code == 401

    async def test_deactivated_user_refresh_fails(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_refresh_token: Callable[..., str],
    ):
        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        refresh_token = create_refresh_token(user_id=test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/refresh",
            json={"refresh_token": refresh_token},
        )

        assert response.status_code == 401


class TestPermissionsInAuthResponse:
    async def test_validate_includes_permissions(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        test_session_factory: Callable,
    ):
        await http_client.put(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
            json={"permissions": {"admin.access": True}},
        )

        session = await test_session_factory(test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )

        assert response.status_code == 200
        data = response.json()
        assert "permissions" in data
        assert data["permissions"].get("admin.access") is True

    async def test_validate_includes_is_active(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        test_session_factory: Callable,
    ):
        session = await test_session_factory(test_user.id)

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )

        assert response.status_code == 200
        data = response.json()
        assert "is_active" in data
        assert data["is_active"] is True
