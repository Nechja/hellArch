import os
from collections.abc import AsyncGenerator
from contextlib import asynccontextmanager

import pytest_asyncio
import redis.asyncio as redis


def get_redis_url() -> str:
    return os.getenv("REDIS_URL", "redis://localhost:6379/0")


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

    async def set_session(self, session_id: str, user_id: str, ttl: int = 3600) -> None:
        await self._client.setex(f"session:{session_id}", ttl, user_id)

    async def get_session(self, session_id: str) -> str | None:
        return await self._client.get(f"session:{session_id}")

    async def delete_session(self, session_id: str) -> None:
        await self._client.delete(f"session:{session_id}")

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
