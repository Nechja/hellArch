"""Pytest fixtures for hellArch testing."""

import os
from collections.abc import AsyncGenerator

import pytest
from pydantic import BaseModel

from testing.clients import (
    AuthClient,
    EventsClient,
    FilesClient,
    GatewayClient,
    ServiceClient,
)
from testing.config import Settings, TestConfig, load_config


class ServiceUrls(BaseModel):
    """Service URLs for testing."""

    gateway: str = "http://gateway:5000"
    auth: str = "http://auth:5001"
    user: str = "http://user:5002"
    events: str = "http://events:5005"
    files: str = "http://files:5006"
    identity: str = "http://identity:5010"


@pytest.fixture(scope="session")
def settings() -> Settings:
    """Load settings from environment."""
    return Settings()


@pytest.fixture(scope="session")
def test_config(settings: Settings) -> TestConfig:
    """Load test configuration."""
    return load_config(settings.config_path)


@pytest.fixture(scope="session")
def service_urls(test_config: TestConfig) -> ServiceUrls:
    """Get service URLs from config."""
    return ServiceUrls(**test_config.services)


@pytest.fixture
async def auth_client(service_urls: ServiceUrls) -> AsyncGenerator[AuthClient, None]:
    """Auth service client."""
    async with AuthClient(service_urls.auth) as client:
        yield client


@pytest.fixture
async def events_client(service_urls: ServiceUrls) -> AsyncGenerator[EventsClient, None]:
    """Events service client."""
    async with EventsClient(service_urls.events) as client:
        yield client


@pytest.fixture
async def files_client(service_urls: ServiceUrls) -> AsyncGenerator[FilesClient, None]:
    """Files service client."""
    async with FilesClient(service_urls.files) as client:
        yield client


@pytest.fixture
async def gateway_client(service_urls: ServiceUrls) -> AsyncGenerator[GatewayClient, None]:
    """Gateway service client."""
    async with GatewayClient(service_urls.gateway) as client:
        yield client


@pytest.fixture(scope="session")
def test_credentials() -> tuple[str, str]:
    """Get test credentials from environment."""
    client_id = os.environ.get("TESTING_CLIENT_ID", "")
    client_secret = os.environ.get("TESTING_CLIENT_SECRET", "")
    return client_id, client_secret


@pytest.fixture
async def auth_token(
    auth_client: AuthClient, test_credentials: tuple[str, str]
) -> str | None:
    """Obtain a valid auth token for testing."""
    client_id, client_secret = test_credentials
    if not client_id or not client_secret:
        pytest.skip("Test credentials not configured")

    token_response = await auth_client.get_token(client_id, client_secret)
    if token_response is None:
        pytest.fail("Failed to obtain auth token")

    return token_response.access_token
