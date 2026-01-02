from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from datetime import UTC, datetime
from uuid import uuid4

import psycopg
import pytest_asyncio


@dataclass
class UserProfile:
    id: str
    auth_user_id: str
    email: str
    display_name: str
    bio: str | None = None
    avatar_url: str | None = None
    created_at: datetime = field(default_factory=lambda: datetime.now(UTC))
    updated_at: datetime = field(default_factory=lambda: datetime.now(UTC))


@dataclass
class UserPreferences:
    id: str
    user_profile_id: str
    theme: str = "dark"
    notifications_enabled: bool = True
    email_notifications: bool = True
    created_at: datetime = field(default_factory=lambda: datetime.now(UTC))
    updated_at: datetime = field(default_factory=lambda: datetime.now(UTC))


async def create_test_profile(
    conn: psycopg.AsyncConnection,
    auth_user_id: str,
    email: str = "test@example.com",
    display_name: str = "Test User",
    bio: str | None = None,
    avatar_url: str | None = None,
) -> UserProfile:
    profile_id = str(uuid4())
    now = datetime.now(UTC)

    await conn.execute(
        """
        INSERT INTO user_profiles (id, auth_user_id, email, display_name, bio, avatar_url, created_at, updated_at)
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
        """,
        (profile_id, auth_user_id, email, display_name, bio, avatar_url, now, now),
    )

    return UserProfile(
        id=profile_id,
        auth_user_id=auth_user_id,
        email=email,
        display_name=display_name,
        bio=bio,
        avatar_url=avatar_url,
        created_at=now,
        updated_at=now,
    )


async def create_test_preferences(
    conn: psycopg.AsyncConnection,
    user_profile_id: str,
    theme: str = "dark",
    notifications_enabled: bool = True,
    email_notifications: bool = True,
) -> UserPreferences:
    pref_id = str(uuid4())
    now = datetime.now(UTC)

    await conn.execute(
        """
        INSERT INTO user_preferences (id, user_profile_id, theme, notifications_enabled, email_notifications, created_at, updated_at)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        """,
        (pref_id, user_profile_id, theme, notifications_enabled, email_notifications, now, now),
    )

    return UserPreferences(
        id=pref_id,
        user_profile_id=user_profile_id,
        theme=theme,
        notifications_enabled=notifications_enabled,
        email_notifications=email_notifications,
        created_at=now,
        updated_at=now,
    )


@pytest_asyncio.fixture
async def test_profile_factory(user_db: psycopg.AsyncConnection) -> Callable[..., Awaitable[UserProfile]]:
    """Factory fixture for creating test user profiles with automatic cleanup."""
    created_profiles: list[str] = []

    async def _create(
        auth_user_id: str,
        email: str = "test@example.com",
        display_name: str = "Test User",
        bio: str | None = None,
        avatar_url: str | None = None,
    ) -> UserProfile:
        profile = await create_test_profile(
            user_db,
            auth_user_id=auth_user_id,
            email=email,
            display_name=display_name,
            bio=bio,
            avatar_url=avatar_url,
        )
        created_profiles.append(profile.id)
        await user_db.commit()
        return profile

    yield _create

    for profile_id in created_profiles:
        await user_db.execute("DELETE FROM user_profiles WHERE id = %s", (profile_id,))
    await user_db.commit()


@pytest_asyncio.fixture
async def test_preferences_factory(user_db: psycopg.AsyncConnection) -> Callable[..., Awaitable[UserPreferences]]:
    """Factory fixture for creating test user preferences with automatic cleanup."""
    created_prefs: list[str] = []

    async def _create(
        user_profile_id: str,
        theme: str = "dark",
        notifications_enabled: bool = True,
        email_notifications: bool = True,
    ) -> UserPreferences:
        prefs = await create_test_preferences(
            user_db,
            user_profile_id=user_profile_id,
            theme=theme,
            notifications_enabled=notifications_enabled,
            email_notifications=email_notifications,
        )
        created_prefs.append(prefs.id)
        await user_db.commit()
        return prefs

    yield _create

    for pref_id in created_prefs:
        await user_db.execute("DELETE FROM user_preferences WHERE id = %s", (pref_id,))
    await user_db.commit()
