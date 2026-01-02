import hashlib
import os
from collections.abc import AsyncGenerator
from contextlib import asynccontextmanager
from datetime import UTC, datetime, timedelta

import pytest_asyncio
import redis.asyncio as redis


def get_redis_url() -> str:
    return os.getenv("REDIS_URL", "redis://localhost:6389/0")


@asynccontextmanager
async def redis_connection() -> AsyncGenerator[redis.Redis, None]:
    client = redis.from_url(get_redis_url(), decode_responses=True)
    try:
        yield client
    finally:
        await client.aclose()


@pytest_asyncio.fixture
async def redis_client() -> AsyncGenerator[redis.Redis, None]:
    async with redis_connection() as client:
        yield client


@pytest_asyncio.fixture
async def clean_redis(redis_client: redis.Redis) -> AsyncGenerator[redis.Redis, None]:
    """Provides a Redis client and flushes the test database after the test."""
    yield redis_client
    await redis_client.flushdb()


class RedisTestHelper:
    """Helper class for common Redis testing operations."""

    def __init__(self, client: redis.Redis):
        self._client = client

    @staticmethod
    def _hash_token(token: str) -> str:
        """Hash token with SHA-256, matching auth service implementation."""
        return hashlib.sha256(token.encode()).hexdigest()

    async def set_session(
        self,
        session_id: str,
        user_id: str,
        token: str,
        ttl: int = 3600,
        ip_address: str = "127.0.0.1",
        user_agent: str = "pytest",
    ) -> None:
        """Store session in Redis matching the auth service format."""
        now = datetime.now(UTC)
        expires_at = now + timedelta(seconds=ttl)
        hashed_token = self._hash_token(token)
        session_key = f"session:{hashed_token}"
        user_sessions_key = f"user:{user_id}:sessions"
        session_id_key = f"session_id:{session_id}"

        pipe = self._client.pipeline()
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
        pipe.expire(session_key, ttl)
        pipe.sadd(user_sessions_key, hashed_token)
        pipe.expire(user_sessions_key, ttl)
        pipe.set(session_id_key, hashed_token, ex=ttl)
        await pipe.execute()

    async def get_session(self, token: str) -> dict[str, str] | None:
        """Get session data by token."""
        hashed_token = self._hash_token(token)
        session_key = f"session:{hashed_token}"
        data = await self._client.hgetall(session_key)
        return data if data else None

    async def delete_session(self, session_id: str, user_id: str, token: str) -> None:
        """Delete session from Redis."""
        hashed_token = self._hash_token(token)
        session_key = f"session:{hashed_token}"
        user_sessions_key = f"user:{user_id}:sessions"
        session_id_key = f"session_id:{session_id}"

        pipe = self._client.pipeline()
        pipe.delete(session_key)
        pipe.srem(user_sessions_key, hashed_token)
        pipe.delete(session_id_key)
        await pipe.execute()

    async def set_rate_limit(self, key: str, count: int, window: int = 60) -> None:
        await self._client.setex(f"ratelimit:{key}", window, count)

    async def get_rate_limit(self, key: str) -> int:
        value = await self._client.get(f"ratelimit:{key}")
        return int(value) if value else 0

    async def increment_rate_limit(self, key: str, window: int = 60) -> int:
        full_key = f"ratelimit:{key}"
        pipe = self._client.pipeline()
        pipe.incr(full_key)
        pipe.expire(full_key, window)
        results = await pipe.execute()
        return results[0]

    async def cache_set(self, key: str, value: str, ttl: int = 300) -> None:
        await self._client.setex(f"cache:{key}", ttl, value)

    async def cache_get(self, key: str) -> str | None:
        return await self._client.get(f"cache:{key}")

    async def cache_delete(self, key: str) -> None:
        await self._client.delete(f"cache:{key}")

    async def publish(self, channel: str, message: str) -> int:
        return await self._client.publish(channel, message)


@pytest_asyncio.fixture
async def redis_helper(redis_client: redis.Redis) -> RedisTestHelper:
    return RedisTestHelper(redis_client)
