import hashlib
import secrets
from collections.abc import Callable
from dataclasses import dataclass
from datetime import UTC, datetime, timedelta
from uuid import uuid4

import psycopg
import pytest_asyncio
import redis.asyncio as redis

from fixtures.db import auth_db_connection
from fixtures.redis import redis_connection


def hash_token(token: str) -> str:
    """Hash token with SHA-256, matching auth service implementation."""
    return hashlib.sha256(token.encode()).hexdigest()


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
    now = datetime.now(UTC)

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


async def create_test_session_in_redis(
    redis_client: redis.Redis,
    user_id: str,
    expires_in: timedelta = timedelta(days=1),
    ip_address: str = "127.0.0.1",
    user_agent: str = "pytest",
) -> AuthSession:
    """Create a session in Redis matching the auth service format."""
    session_id = str(uuid4())
    token = secrets.token_urlsafe(32)
    now = datetime.now(UTC)
    expires_at = now + expires_in
    ttl_seconds = int(expires_in.total_seconds())

    hashed_token = hash_token(token)
    session_key = f"session:{hashed_token}"
    user_sessions_key = f"user:{user_id}:sessions"
    session_id_key = f"session_id:{session_id}"

    pipe = redis_client.pipeline()

    # Store session data as hash (matching Go auth service format)
    pipe.hset(
        session_key,
        mapping={
            "id": session_id,
            "user_id": user_id,
            "ip_address": ip_address,
            "user_agent": user_agent,
            "created_at": now.isoformat(),
            "expires_at": expires_at.isoformat(),
            "token_hash": hashed_token,
        },
    )
    pipe.expire(session_key, ttl_seconds)

    # Add to user's session set
    pipe.sadd(user_sessions_key, hashed_token)
    pipe.expire(user_sessions_key, ttl_seconds)

    # Store session ID to hash mapping
    pipe.set(session_id_key, hashed_token, ex=ttl_seconds)

    await pipe.execute()

    return AuthSession(id=session_id, user_id=user_id, token=token, expires_at=expires_at)


async def delete_test_session_from_redis(
    redis_client: redis.Redis,
    session: AuthSession,
) -> None:
    """Delete a session from Redis."""
    hashed_token = hash_token(session.token)
    session_key = f"session:{hashed_token}"
    user_sessions_key = f"user:{session.user_id}:sessions"
    session_id_key = f"session_id:{session.id}"

    pipe = redis_client.pipeline()
    pipe.delete(session_key)
    pipe.srem(user_sessions_key, hashed_token)
    pipe.delete(session_id_key)
    await pipe.execute()


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
    """Create a test session in Redis for the test user."""
    async with redis_connection() as redis_client:
        session = await create_test_session_in_redis(redis_client, test_user.id)
        yield session
        # Cleanup
        await delete_test_session_from_redis(redis_client, session)


@pytest_asyncio.fixture
async def test_session_factory() -> Callable:
    """Factory fixture for creating multiple test sessions."""
    created_sessions: list[AuthSession] = []

    async def _create(user_id: str) -> AuthSession:
        async with redis_connection() as redis_client:
            session = await create_test_session_in_redis(redis_client, user_id)
            created_sessions.append(session)
            return session

    yield _create

    # Cleanup all created sessions
    async with redis_connection() as redis_client:
        for session in created_sessions:
            await delete_test_session_from_redis(redis_client, session)
