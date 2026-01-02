"""
Events API test fixture.

Unlike other fixtures that only work with the test rig, this fixture is designed
to work with both live systems and test environments by using configurable URLs.
"""

import os
from collections.abc import AsyncGenerator
from dataclasses import dataclass
from datetime import UTC, datetime
from typing import Any

import httpx
import pytest_asyncio

from fixtures.rabbitmq import CloudEvent


def get_events_url() -> str:
    """Get the Events API URL, supporting both live and test environments."""
    return os.getenv("EVENTS_URL", "http://localhost:5005")


def get_gateway_url() -> str:
    """Get the Gateway URL for testing through the gateway."""
    return os.getenv("GATEWAY_URL", "http://localhost:5000")


@dataclass
class AuditLog:
    """Represents an audit log entry from the Events API."""

    id: str
    event_id: str
    event_type: str
    source: str
    actor_id: str | None
    target_id: str | None
    metadata: dict[str, Any] | None
    created_at: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "AuditLog":
        return cls(
            id=data["id"],
            event_id=data["event_id"],
            event_type=data["event_type"],
            source=data["source"],
            actor_id=data.get("actor_id"),
            target_id=data.get("target_id"),
            metadata=data.get("metadata"),
            created_at=data["created_at"],
        )


@dataclass
class Webhook:
    """Represents a webhook configuration."""

    id: str
    name: str
    url: str
    event_types: list[str]
    secret: str | None
    is_active: bool
    created_at: str
    updated_at: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "Webhook":
        return cls(
            id=data["id"],
            name=data["name"],
            url=data["url"],
            event_types=data["event_types"],
            secret=data.get("secret"),
            is_active=data["is_active"],
            created_at=data["created_at"],
            updated_at=data["updated_at"],
        )


@dataclass
class WebhookDelivery:
    """Represents a webhook delivery attempt."""

    id: str
    webhook_id: str
    event_id: str
    event_type: str
    status_code: int
    response_body: str | None
    success: bool
    attempt: int
    created_at: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "WebhookDelivery":
        return cls(
            id=data["id"],
            webhook_id=data["webhook_id"],
            event_id=data["event_id"],
            event_type=data["event_type"],
            status_code=data["status_code"],
            response_body=data.get("response_body"),
            success=data["success"],
            attempt=data["attempt"],
            created_at=data["created_at"],
        )


@dataclass
class PaginatedResponse:
    """Generic paginated response."""

    data: list[Any]
    total_count: int
    page: int
    page_size: int


class EventsAPIHelper:
    """
    Helper class for testing the Events API.

    Supports both direct API access and gateway-proxied access.
    Can be used against live systems or test environments.
    """

    def __init__(self, client: httpx.AsyncClient, base_url: str | None = None, use_gateway: bool = False):
        self._client = client
        self._use_gateway = use_gateway

        if base_url:
            self._base_url = base_url
        elif use_gateway:
            self._base_url = f"{get_gateway_url()}/api/events"
        else:
            self._base_url = get_events_url()

    async def get_audit_logs(
        self,
        page: int = 1,
        page_size: int = 20,
        event_type: str | None = None,
        actor_id: str | None = None,
    ) -> PaginatedResponse:
        """Get audit logs with optional filtering."""
        params: dict[str, Any] = {"page": page, "page_size": page_size}
        if event_type:
            params["event_type"] = event_type
        if actor_id:
            params["actor_id"] = actor_id

        response = await self._client.get(f"{self._base_url}/api/audit-logs", params=params)
        response.raise_for_status()
        data = response.json()

        logs = data.get("data") or []
        return PaginatedResponse(
            data=[AuditLog.from_dict(log) for log in logs],
            total_count=data.get("total", 0),
            page=page,
            page_size=page_size,
        )

    async def get_audit_log(self, log_id: str) -> AuditLog:
        """Get a specific audit log by ID."""
        response = await self._client.get(f"{self._base_url}/api/audit-logs/{log_id}")
        response.raise_for_status()
        return AuditLog.from_dict(response.json())

    async def create_audit_log(
        self,
        event_id: str,
        event_type: str,
        source: str,
        actor_id: str | None = None,
        target_id: str | None = None,
        metadata: dict[str, Any] | None = None,
    ) -> AuditLog:
        """Create an audit log entry (used by audit worker)."""
        payload = {
            "event_id": event_id,
            "event_type": event_type,
            "source": source,
        }
        if actor_id:
            payload["actor_id"] = actor_id
        if target_id:
            payload["target_id"] = target_id
        if metadata:
            payload["metadata"] = metadata

        response = await self._client.post(f"{self._base_url}/api/audit-logs", json=payload)
        response.raise_for_status()
        return AuditLog.from_dict(response.json())

    async def get_webhooks(self, page: int = 1, page_size: int = 20) -> PaginatedResponse:
        """Get all webhooks."""
        params = {"page": page, "page_size": page_size}
        response = await self._client.get(f"{self._base_url}/api/webhooks", params=params)
        response.raise_for_status()
        data = response.json()

        webhooks = data.get("data") or []
        return PaginatedResponse(
            data=[Webhook.from_dict(w) for w in webhooks],
            total_count=data.get("total", 0),
            page=page,
            page_size=page_size,
        )

    async def get_webhook(self, webhook_id: str) -> Webhook:
        """Get a specific webhook by ID."""
        response = await self._client.get(f"{self._base_url}/api/webhooks/{webhook_id}")
        response.raise_for_status()
        return Webhook.from_dict(response.json())

    async def create_webhook(
        self,
        name: str,
        url: str,
        secret: str,
        event_types: list[str],
    ) -> Webhook:
        """Create a new webhook. Name, URL, and secret are required."""
        payload: dict[str, Any] = {
            "name": name,
            "url": url,
            "secret": secret,
            "event_types": event_types,
        }

        response = await self._client.post(f"{self._base_url}/api/webhooks", json=payload)
        response.raise_for_status()
        return Webhook.from_dict(response.json())

    async def update_webhook(
        self,
        webhook_id: str,
        name: str | None = None,
        url: str | None = None,
        event_types: list[str] | None = None,
        secret: str | None = None,
        is_active: bool | None = None,
    ) -> Webhook:
        """Update an existing webhook."""
        payload: dict[str, Any] = {}
        if name is not None:
            payload["name"] = name
        if url is not None:
            payload["url"] = url
        if event_types is not None:
            payload["event_types"] = event_types
        if secret is not None:
            payload["secret"] = secret
        if is_active is not None:
            payload["is_active"] = is_active

        response = await self._client.put(f"{self._base_url}/api/webhooks/{webhook_id}", json=payload)
        response.raise_for_status()
        return Webhook.from_dict(response.json())

    async def delete_webhook(self, webhook_id: str) -> None:
        """Delete a webhook."""
        response = await self._client.delete(f"{self._base_url}/api/webhooks/{webhook_id}")
        response.raise_for_status()

    async def get_webhooks_by_event_type(self, event_type: str) -> list[Webhook]:
        """Get all active webhooks subscribed to a specific event type."""
        response = await self._client.get(f"{self._base_url}/api/webhooks/by-event/{event_type}")
        response.raise_for_status()
        data = response.json()
        webhooks = data.get("data") or []
        return [Webhook.from_dict(w) for w in webhooks]

    async def get_deliveries(
        self,
        page: int = 1,
        page_size: int = 20,
        webhook_id: str | None = None,
    ) -> PaginatedResponse:
        """Get webhook deliveries with optional filtering."""
        params: dict[str, Any] = {"page": page, "page_size": page_size}
        if webhook_id:
            params["webhook_id"] = webhook_id

        response = await self._client.get(f"{self._base_url}/api/webhook-deliveries", params=params)
        response.raise_for_status()
        data = response.json()

        deliveries = data.get("data") or []
        return PaginatedResponse(
            data=[WebhookDelivery.from_dict(d) for d in deliveries],
            total_count=data.get("total", 0),
            page=page,
            page_size=page_size,
        )

    async def get_delivery(self, delivery_id: str) -> WebhookDelivery:
        """Get a specific delivery by ID."""
        response = await self._client.get(f"{self._base_url}/api/webhook-deliveries/{delivery_id}")
        response.raise_for_status()
        return WebhookDelivery.from_dict(response.json())

    async def create_delivery(
        self,
        webhook_id: str,
        event_id: str,
        event_type: str,
        status_code: int = 0,
        response_body: str | None = None,
        success: bool = False,
        attempt: int = 1,
    ) -> WebhookDelivery:
        """Record a webhook delivery attempt (used by webhooks worker)."""
        payload: dict[str, Any] = {
            "webhook_id": webhook_id,
            "event_id": event_id,
            "event_type": event_type,
            "status_code": status_code,
            "success": success,
            "attempt": attempt,
        }
        if response_body is not None:
            payload["response_body"] = response_body

        response = await self._client.post(f"{self._base_url}/api/webhook-deliveries", json=payload)
        response.raise_for_status()
        return WebhookDelivery.from_dict(response.json())

    async def publish_event(self, event: CloudEvent) -> dict[str, Any]:
        """Publish an event through the Events API (for services without RabbitMQ access)."""
        response = await self._client.post(f"{self._base_url}/api/events/publish", json=event.to_dict())
        response.raise_for_status()
        # API returns 202 Accepted with no body
        return {"status": "accepted"}

    async def health_check(self) -> dict[str, Any]:
        """Check the Events API health."""
        response = await self._client.get(f"{self._base_url}/health")
        response.raise_for_status()
        return response.json()


@pytest_asyncio.fixture
async def events_helper(http_client: httpx.AsyncClient) -> EventsAPIHelper:
    """
    Fixture providing an Events API helper for direct API access.

    Note: This fixture will fail if the Events API service is not running.
    The Events API service is created in Phase 3 of the events system implementation.
    """
    helper = EventsAPIHelper(http_client)

    try:
        await helper.health_check()
    except httpx.ConnectError:
        import pytest

        pytest.skip("Events API service not available - skipping test")

    return helper


@pytest_asyncio.fixture
async def events_helper_via_gateway(http_client: httpx.AsyncClient) -> EventsAPIHelper:
    """
    Fixture providing an Events API helper that goes through the gateway.

    Note: This fixture requires both the Gateway and Events API service to be running.
    """
    helper = EventsAPIHelper(http_client, use_gateway=True)

    try:
        await helper.health_check()
    except httpx.ConnectError:
        import pytest

        pytest.skip("Events API service (via gateway) not available - skipping test")

    return helper


@pytest_asyncio.fixture
async def webhook_factory(
    events_helper: EventsAPIHelper,
) -> AsyncGenerator[list[Webhook], None]:
    """Factory fixture for creating test webhooks with automatic cleanup."""
    created_webhooks: list[Webhook] = []

    async def _create(
        name: str = "test-webhook",
        url: str = "https://example.com/webhook",
        secret: str = "test-secret",  # noqa: S107
        event_types: list[str] | None = None,
    ) -> Webhook:
        if event_types is None:
            event_types = ["hellarch.test.event.v1"]

        webhook = await events_helper.create_webhook(
            name=name,
            url=url,
            secret=secret,
            event_types=event_types,
        )
        created_webhooks.append(webhook)
        return webhook

    yield created_webhooks

    for webhook in created_webhooks:
        try:
            await events_helper.delete_webhook(webhook.id)
        except httpx.HTTPStatusError:
            pass


def create_auth_user_created_event(
    user_id: str,
    email: str,
    username: str,
    provider: str = "discord",
) -> CloudEvent:
    """Create a user created event matching auth service format."""
    return CloudEvent(
        type="hellarch.auth.user.created.v1",
        source="auth-service",
        data={
            "user_id": user_id,
            "email": email,
            "username": username,
            "provider": provider,
        },
    )


def create_auth_user_updated_event(
    user_id: str,
    changes: dict[str, Any],
) -> CloudEvent:
    """Create a user updated event."""
    return CloudEvent(
        type="hellarch.auth.user.updated.v1",
        source="auth-service",
        data={
            "user_id": user_id,
            "changes": changes,
        },
    )


def create_auth_user_deleted_event(user_id: str) -> CloudEvent:
    """Create a user deleted event."""
    return CloudEvent(
        type="hellarch.auth.user.deleted.v1",
        source="auth-service",
        data={
            "user_id": user_id,
        },
    )


def create_auth_session_created_event(
    session_id: str,
    user_id: str,
    ip_address: str = "127.0.0.1",
) -> CloudEvent:
    """Create a session created event."""
    return CloudEvent(
        type="hellarch.auth.session.created.v1",
        source="auth-service",
        data={
            "session_id": session_id,
            "user_id": user_id,
            "ip_address": ip_address,
        },
    )


def create_auth_session_ended_event(
    session_id: str,
    user_id: str,
    reason: str = "logout",
) -> CloudEvent:
    """Create a session ended event."""
    return CloudEvent(
        type="hellarch.auth.session.ended.v1",
        source="auth-service",
        data={
            "session_id": session_id,
            "user_id": user_id,
            "reason": reason,
        },
    )


def create_auth_account_linked_event(
    user_id: str,
    provider: str,
    provider_user_id: str,
) -> CloudEvent:
    """Create an account linked event."""
    return CloudEvent(
        type="hellarch.auth.account.linked.v1",
        source="auth-service",
        data={
            "user_id": user_id,
            "provider": provider,
            "provider_user_id": provider_user_id,
        },
    )


def create_auth_account_unlinked_event(
    user_id: str,
    provider: str,
) -> CloudEvent:
    """Create an account unlinked event."""
    return CloudEvent(
        type="hellarch.auth.account.unlinked.v1",
        source="auth-service",
        data={
            "user_id": user_id,
            "provider": provider,
        },
    )


def create_infra_service_unhealthy_event(
    service_name: str,
    reason: str,
    details: dict[str, Any] | None = None,
) -> CloudEvent:
    """Create a service unhealthy event."""
    data: dict[str, Any] = {
        "service_name": service_name,
        "reason": reason,
    }
    if details:
        data["details"] = details

    return CloudEvent(
        type="hellarch.infra.service.unhealthy.v1",
        source="health-service",
        data=data,
    )


def create_infra_service_recovered_event(
    service_name: str,
    downtime_seconds: int | None = None,
) -> CloudEvent:
    """Create a service recovered event."""
    data: dict[str, Any] = {
        "service_name": service_name,
    }
    if downtime_seconds is not None:
        data["downtime_seconds"] = downtime_seconds

    return CloudEvent(
        type="hellarch.infra.service.recovered.v1",
        source="health-service",
        data=data,
    )


def create_test_event(
    event_type: str = "hellarch.test.event.v1",
    source: str = "test-suite",
    data: dict[str, Any] | None = None,
) -> CloudEvent:
    """Create a generic test event."""
    return CloudEvent(
        type=event_type,
        source=source,
        data=data or {"test": True, "timestamp": datetime.now(UTC).isoformat()},
    )


ALL_EVENT_TYPES = [
    "hellarch.auth.user.created.v1",
    "hellarch.auth.user.updated.v1",
    "hellarch.auth.user.deleted.v1",
    "hellarch.auth.session.created.v1",
    "hellarch.auth.session.ended.v1",
    "hellarch.auth.account.linked.v1",
    "hellarch.auth.account.unlinked.v1",
    "hellarch.infra.service.unhealthy.v1",
    "hellarch.infra.service.recovered.v1",
]
