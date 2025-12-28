import secrets
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Callable
from uuid import uuid4

import psycopg
import pytest_asyncio

from fixtures.db import auth_db_connection


@dataclass
class AuthUser:
    id: str
    email: str
    username: str
    provider: str = "discord"


@dataclass
class AuthSession:
    id: str
    user_id: str
    token: str
    expires_at: datetime


async def create_test_user(
    conn: psycopg.AsyncConnection,
    email: str = "test@example.com",
    username: str = "testuser",
    provider: str = "discord",
) -> AuthUser:
    user_id = str(uuid4())
    provider_user_id = str(uuid4())
    now = datetime.now(timezone.utc)

    await conn.execute(
        """
        INSERT INTO users (id, email, username, created_at, updated_at)
        VALUES (%s, %s, %s, %s, %s)
        """,
        (user_id, email, username, now, now),
    )

    await conn.execute(
        """
        INSERT INTO linked_accounts (id, user_id, provider, provider_user_id, email, username, created_at)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        """,
        (str(uuid4()), user_id, provider, provider_user_id, email, username, now),
    )

    return AuthUser(id=user_id, email=email, username=username, provider=provider)


async def create_test_session(
    conn: psycopg.AsyncConnection,
    user_id: str,
    expires_in: timedelta = timedelta(days=30),
    ip_address: str = "127.0.0.1",
    user_agent: str = "pytest",
) -> AuthSession:
    session_id = str(uuid4())
    token = secrets.token_urlsafe(32)
    now = datetime.now(timezone.utc)
    expires_at = now + expires_in

    await conn.execute(
        """
        INSERT INTO sessions (id, user_id, token, expires_at, created_at, ip_address, user_agent)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        """,
        (session_id, user_id, token, expires_at, now, ip_address, user_agent),
    )

    return AuthSession(id=session_id, user_id=user_id, token=token, expires_at=expires_at)


@pytest_asyncio.fixture
async def test_user_factory() -> Callable[..., AuthUser]:
    created_users: list[str] = []

    async def _create(email: str = "test@example.com", username: str = "testuser") -> AuthUser:
        async with auth_db_connection() as conn:
            user = await create_test_user(conn, email, username)
            created_users.append(user.id)
            await conn.commit()
            return user

    yield _create

    async with auth_db_connection() as conn:
        for user_id in created_users:
            await conn.execute("DELETE FROM users WHERE id = %s", (user_id,))
        await conn.commit()


@pytest_asyncio.fixture
async def test_user(test_user_factory: Callable[..., AuthUser]) -> AuthUser:
    return await test_user_factory()


@pytest_asyncio.fixture
async def test_session(test_user: AuthUser) -> AuthSession:
    async with auth_db_connection() as conn:
        session = await create_test_session(conn, test_user.id)
        await conn.commit()
        return session
