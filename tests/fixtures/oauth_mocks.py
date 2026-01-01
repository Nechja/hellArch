from collections.abc import Generator
from dataclasses import dataclass

import pytest
import respx
from httpx import Response


@dataclass
class MockOAuthUser:
    id: str
    email: str
    username: str
    avatar_url: str


# OAuth provider endpoints (not secrets, just API URLs)
DISCORD_TOKEN_URL = "https://discord.com/api/oauth2/token"  # noqa: S105
DISCORD_USER_URL = "https://discord.com/api/users/@me"
GOOGLE_TOKEN_URL = "https://oauth2.googleapis.com/token"  # noqa: S105
GOOGLE_USER_URL = "https://www.googleapis.com/oauth2/v2/userinfo"
GITHUB_TOKEN_URL = "https://github.com/login/oauth/access_token"  # noqa: S105
GITHUB_USER_URL = "https://api.github.com/user"


@pytest.fixture
def mock_discord_oauth() -> Generator[tuple[respx.MockRouter, MockOAuthUser], None, None]:
    user = MockOAuthUser(
        id="123456789",
        email="testuser@example.com",
        username="TestUser",
        avatar_url="https://cdn.discordapp.com/avatars/123456789/abc123.png",
    )

    with respx.mock(assert_all_called=False) as mock:
        mock.post(DISCORD_TOKEN_URL).respond(
            json={
                "access_token": "mock_discord_access_token",
                "token_type": "Bearer",
                "expires_in": 604800,
                "refresh_token": "mock_discord_refresh_token",
                "scope": "identify email",
            }
        )

        mock.get(DISCORD_USER_URL).respond(
            json={
                "id": user.id,
                "username": user.username,
                "discriminator": "0",
                "email": user.email,
                "avatar": "abc123",
            }
        )

        yield mock, user


@pytest.fixture
def mock_google_oauth() -> Generator[tuple[respx.MockRouter, MockOAuthUser], None, None]:
    user = MockOAuthUser(
        id="987654321",
        email="testuser@gmail.com",
        username="Test User",
        avatar_url="https://lh3.googleusercontent.com/photo.jpg",
    )

    with respx.mock(assert_all_called=False) as mock:
        mock.post(GOOGLE_TOKEN_URL).respond(
            json={
                "access_token": "mock_google_access_token",
                "token_type": "Bearer",
                "expires_in": 3600,
                "refresh_token": "mock_google_refresh_token",
                "scope": "openid email profile",
            }
        )

        mock.get(GOOGLE_USER_URL).respond(
            json={
                "id": user.id,
                "email": user.email,
                "name": user.username,
                "picture": user.avatar_url,
            }
        )

        yield mock, user


@pytest.fixture
def mock_github_oauth() -> Generator[tuple[respx.MockRouter, MockOAuthUser], None, None]:
    user = MockOAuthUser(
        id="456789",
        email="testuser@github.com",
        username="testuser",
        avatar_url="https://avatars.githubusercontent.com/u/456789",
    )

    with respx.mock(assert_all_called=False) as mock:
        mock.post(GITHUB_TOKEN_URL).respond(
            json={
                "access_token": "mock_github_access_token",
                "token_type": "Bearer",
                "scope": "read:user,user:email",
            }
        )

        mock.get(GITHUB_USER_URL).respond(
            json={
                "id": int(user.id),
                "login": user.username,
                "email": user.email,
                "avatar_url": user.avatar_url,
            }
        )

        yield mock, user


def create_failed_token_response() -> Response:
    return Response(
        status_code=400,
        json={"error": "invalid_grant", "error_description": "Invalid authorization code"},
    )
