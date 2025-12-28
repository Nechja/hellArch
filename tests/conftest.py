import os
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

import httpx
import pytest
from jose import jwt

PROJECT_ROOT = Path(__file__).parent.parent


@pytest.fixture(scope="session")
def base_urls() -> dict[str, str]:
    return {
        "gateway": os.getenv("GATEWAY_URL", "http://localhost:5000"),
        "auth": os.getenv("AUTH_URL", "http://localhost:5001"),
        "user": os.getenv("USER_URL", "http://localhost:5002"),
        "admin": os.getenv("ADMIN_URL", "http://localhost:5003"),
    }


@pytest.fixture(scope="session")
def jwt_keys() -> tuple[str, str]:
    private_key_path = PROJECT_ROOT / "infra" / "keys" / "dev-private.pem"
    public_key_path = PROJECT_ROOT / "infra" / "keys" / "dev-public.pem"

    private_key = private_key_path.read_text()
    public_key = public_key_path.read_text()

    return private_key, public_key


@pytest.fixture
async def http_client() -> httpx.AsyncClient:
    async with httpx.AsyncClient(timeout=30.0) as client:
        yield client


@pytest.fixture
def create_access_token(jwt_keys: tuple[str, str]):
    private_key, _ = jwt_keys

    def _create(
        user_id: str,
        email: str = "test@example.com",
        username: str = "testuser",
        provider: str = "discord",
        expires_in: timedelta = timedelta(hours=1),
        **extra_claims: Any,
    ) -> str:
        now = datetime.now(timezone.utc)
        claims = {
            "iss": "hellarch-auth",
            "sub": user_id,
            "iat": now,
            "exp": now + expires_in,
            "nbf": now,
            "uid": user_id,
            "email": email,
            "username": username,
            "provider": provider,
            **extra_claims,
        }
        return jwt.encode(claims, private_key, algorithm="RS256")

    return _create


@pytest.fixture
def create_refresh_token(jwt_keys: tuple[str, str]):
    private_key, _ = jwt_keys

    def _create(
        user_id: str,
        expires_in: timedelta = timedelta(days=30),
    ) -> str:
        now = datetime.now(timezone.utc)
        claims = {
            "iss": "hellarch-auth",
            "sub": user_id,
            "iat": now,
            "exp": now + expires_in,
            "nbf": now,
        }
        return jwt.encode(claims, private_key, algorithm="RS256")

    return _create
