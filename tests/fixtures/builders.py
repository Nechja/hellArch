from dataclasses import dataclass, field
from datetime import UTC, datetime, timedelta
from enum import Enum
from typing import Any, Self
from uuid import uuid4

from faker import Faker

fake = Faker()


class OAuthProvider(Enum):
    DISCORD = "discord"
    GOOGLE = "google"
    GITHUB = "github"


@dataclass
class UserData:
    """Test data for a user."""

    id: str = field(default_factory=lambda: str(uuid4()))
    email: str = field(default_factory=fake.email)
    username: str = field(default_factory=fake.user_name)
    provider: str = "discord"
    provider_user_id: str = field(default_factory=lambda: str(uuid4()))
    avatar_url: str | None = None
    is_active: bool = True
    permissions: dict[str, bool] = field(default_factory=dict)
    created_at: datetime = field(default_factory=lambda: datetime.now(UTC))


@dataclass
class SessionData:
    """Test data for a session."""

    id: str = field(default_factory=lambda: str(uuid4()))
    user_id: str = field(default_factory=lambda: str(uuid4()))
    token: str = field(default_factory=lambda: fake.sha256())
    ip_address: str = field(default_factory=fake.ipv4)
    user_agent: str = field(default_factory=fake.user_agent)
    expires_at: datetime = field(
        default_factory=lambda: datetime.now(UTC) + timedelta(days=30)
    )
    created_at: datetime = field(default_factory=lambda: datetime.now(UTC))


@dataclass
class ProfileData:
    """Test data for a user profile."""

    id: str = field(default_factory=lambda: str(uuid4()))
    auth_user_id: str = field(default_factory=lambda: str(uuid4()))
    email: str = field(default_factory=fake.email)
    display_name: str = field(default_factory=fake.name)
    bio: str | None = None
    avatar_url: str | None = None
    created_at: datetime = field(default_factory=lambda: datetime.now(UTC))


@dataclass
class PreferencesData:
    """Test data for user preferences."""

    id: str = field(default_factory=lambda: str(uuid4()))
    user_profile_id: str = field(default_factory=lambda: str(uuid4()))
    theme: str = "dark"
    notifications_enabled: bool = True
    email_notifications: bool = True


class UserBuilder:
    """Builder for creating test user data with fluent interface."""

    def __init__(self):
        self._data = UserData()

    def with_id(self, user_id: str) -> Self:
        self._data.id = user_id
        return self

    def with_email(self, email: str) -> Self:
        self._data.email = email
        return self

    def with_username(self, username: str) -> Self:
        self._data.username = username
        return self

    def with_provider(self, provider: str | OAuthProvider) -> Self:
        self._data.provider = provider.value if isinstance(provider, OAuthProvider) else provider
        return self

    def with_avatar(self, url: str) -> Self:
        self._data.avatar_url = url
        return self

    def inactive(self) -> Self:
        self._data.is_active = False
        return self

    def with_permission(self, permission: str, granted: bool = True) -> Self:
        self._data.permissions[permission] = granted
        return self

    def with_permissions(self, permissions: dict[str, bool]) -> Self:
        self._data.permissions.update(permissions)
        return self

    def admin(self) -> Self:
        return self.with_permissions({
            "admin.access": True,
            "admin.users.read": True,
            "admin.users.write": True,
        })

    def build(self) -> UserData:
        return self._data

    def as_dict(self) -> dict[str, Any]:
        return {
            "id": self._data.id,
            "email": self._data.email,
            "username": self._data.username,
            "provider": self._data.provider,
            "provider_user_id": self._data.provider_user_id,
            "avatar_url": self._data.avatar_url,
            "is_active": self._data.is_active,
            "permissions": self._data.permissions,
        }

    def as_create_request(self) -> dict[str, Any]:
        return {
            "email": self._data.email,
            "username": self._data.username,
            "provider": self._data.provider,
            "provider_user_id": self._data.provider_user_id,
        }


class SessionBuilder:
    """Builder for creating test session data."""

    def __init__(self):
        self._data = SessionData()

    def for_user(self, user_id: str) -> Self:
        self._data.user_id = user_id
        return self

    def with_token(self, token: str) -> Self:
        self._data.token = token
        return self

    def with_ip(self, ip: str) -> Self:
        self._data.ip_address = ip
        return self

    def with_user_agent(self, user_agent: str) -> Self:
        self._data.user_agent = user_agent
        return self

    def expires_in(self, delta: timedelta) -> Self:
        self._data.expires_at = datetime.now(UTC) + delta
        return self

    def expired(self) -> Self:
        self._data.expires_at = datetime.now(UTC) - timedelta(hours=1)
        return self

    def build(self) -> SessionData:
        return self._data


class ProfileBuilder:
    """Builder for creating test profile data."""

    def __init__(self):
        self._data = ProfileData()

    def for_auth_user(self, auth_user_id: str) -> Self:
        self._data.auth_user_id = auth_user_id
        return self

    def with_email(self, email: str) -> Self:
        self._data.email = email
        return self

    def with_display_name(self, name: str) -> Self:
        self._data.display_name = name
        return self

    def with_bio(self, bio: str) -> Self:
        self._data.bio = bio
        return self

    def with_avatar(self, url: str) -> Self:
        self._data.avatar_url = url
        return self

    def build(self) -> ProfileData:
        return self._data

    def as_create_request(self) -> dict[str, Any]:
        return {
            "user_id": self._data.auth_user_id,
            "email": self._data.email,
            "display_name": self._data.display_name,
            "bio": self._data.bio,
            "avatar_url": self._data.avatar_url,
        }

    def as_update_request(self) -> dict[str, Any]:
        data: dict[str, Any] = {}
        if self._data.display_name is not None:
            data["display_name"] = self._data.display_name
        if self._data.bio is not None:
            data["bio"] = self._data.bio
        if self._data.avatar_url is not None:
            data["avatar_url"] = self._data.avatar_url
        return data


class PreferencesBuilder:
    """Builder for creating test preferences data."""

    def __init__(self):
        self._data = PreferencesData()

    def for_profile(self, profile_id: str) -> Self:
        self._data.user_profile_id = profile_id
        return self

    def light_theme(self) -> Self:
        self._data.theme = "light"
        return self

    def dark_theme(self) -> Self:
        self._data.theme = "dark"
        return self

    def with_theme(self, theme: str) -> Self:
        self._data.theme = theme
        return self

    def notifications_disabled(self) -> Self:
        self._data.notifications_enabled = False
        return self

    def email_notifications_disabled(self) -> Self:
        self._data.email_notifications = False
        return self

    def build(self) -> PreferencesData:
        return self._data

    def as_update_request(self) -> dict[str, Any]:
        return {
            "theme": self._data.theme,
            "notifications_enabled": self._data.notifications_enabled,
            "email_notifications": self._data.email_notifications,
        }


def user() -> UserBuilder:
    """Create a new user builder."""
    return UserBuilder()


def session() -> SessionBuilder:
    """Create a new session builder."""
    return SessionBuilder()


def profile() -> ProfileBuilder:
    """Create a new profile builder."""
    return ProfileBuilder()


def preferences() -> PreferencesBuilder:
    """Create a new preferences builder."""
    return PreferencesBuilder()


def random_email() -> str:
    return fake.email()


def random_username() -> str:
    return fake.user_name()


def random_uuid() -> str:
    return str(uuid4())


def random_ip() -> str:
    return fake.ipv4()


def random_user_agent() -> str:
    return fake.user_agent()


def random_bio() -> str:
    return fake.paragraph(nb_sentences=2)


def random_display_name() -> str:
    return fake.name()
