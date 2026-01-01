import os
from collections.abc import AsyncGenerator
from contextlib import asynccontextmanager

import psycopg
import pytest_asyncio


def get_auth_db_url() -> str:
    return os.getenv(
        "AUTH_DATABASE_URL",
        "postgres://hellarch:hellarch@localhost:5444/auth_db",
    )


def get_user_db_url() -> str:
    return os.getenv(
        "USER_DATABASE_URL",
        "postgres://hellarch:hellarch@localhost:5443/user_db",
    )


@asynccontextmanager
async def auth_db_connection() -> AsyncGenerator[psycopg.AsyncConnection, None]:
    async with await psycopg.AsyncConnection.connect(get_auth_db_url()) as conn:
        yield conn


@asynccontextmanager
async def user_db_connection() -> AsyncGenerator[psycopg.AsyncConnection, None]:
    async with await psycopg.AsyncConnection.connect(get_user_db_url()) as conn:
        yield conn


@pytest_asyncio.fixture
async def auth_db() -> AsyncGenerator[psycopg.AsyncConnection, None]:
    async with auth_db_connection() as conn:
        yield conn


@pytest_asyncio.fixture
async def user_db() -> AsyncGenerator[psycopg.AsyncConnection, None]:
    async with user_db_connection() as conn:
        yield conn


async def cleanup_auth_db(conn: psycopg.AsyncConnection) -> None:
    await conn.execute("TRUNCATE sessions, linked_accounts, users CASCADE")


async def cleanup_user_db(conn: psycopg.AsyncConnection) -> None:
    await conn.execute("TRUNCATE user_preferences, user_profiles CASCADE")


@pytest_asyncio.fixture
async def clean_auth_db(auth_db: psycopg.AsyncConnection) -> AsyncGenerator[psycopg.AsyncConnection, None]:
    yield auth_db
    await cleanup_auth_db(auth_db)


@pytest_asyncio.fixture
async def clean_user_db(user_db: psycopg.AsyncConnection) -> AsyncGenerator[psycopg.AsyncConnection, None]:
    yield user_db
    await cleanup_user_db(user_db)
