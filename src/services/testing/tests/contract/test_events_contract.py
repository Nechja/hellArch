"""
Contract tests for Events service API via Gateway.

These tests verify that API responses match expected schemas.
All requests go through the gateway at /api/events/*.
"""

from uuid import uuid4

import pytest

from testing.clients import GatewayClient
from testing.schemas import (
    AUDIT_LOG_SCHEMA,
    AUDIT_LOGS_RESPONSE,
    CLOUD_EVENT_SCHEMA,
    HEALTH_SCHEMA,
    WEBHOOK_SCHEMA,
)


@pytest.mark.contract
class TestEventsHealthContract:
    """Contract tests for events health endpoint."""

    async def test_health_response_schema(self, gateway_client: GatewayClient) -> None:
        """Health endpoint returns expected schema."""
        # Events health is at the service level, not through gateway
        # Gateway routes /api/events/* -> events service
        response = await gateway_client.get("/api/events/health")

        if response.status_code == 404:
            pytest.skip("Events health endpoint not exposed via gateway")

        assert response.status_code == 200

        data = response.json()
        errors = HEALTH_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"


@pytest.mark.contract
class TestAuditLogsContract:
    """Contract tests for audit log endpoints."""

    async def test_get_audit_logs_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """GET /api/events/audit-logs returns paginated response."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            "/api/events/audit-logs",
            params={"page": 1, "page_size": 10},
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Audit logs require authentication")

        assert response.status_code == 200

        data = response.json()
        errors = AUDIT_LOGS_RESPONSE.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert isinstance(data.get("data"), list)

    async def test_create_audit_log_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """POST /api/events/audit-logs returns created log."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "event_id": str(uuid4()),
            "event_type": "hellarch.test.contract.v1",
            "source": "contract-test",
            "metadata": {"test": True},
        }

        response = await gateway_client.post(
            "/api/events/audit-logs",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Creating audit logs requires authentication")

        assert response.status_code == 201

        data = response.json()
        errors = AUDIT_LOG_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"
        assert data["event_type"] == payload["event_type"]


@pytest.mark.contract
class TestWebhooksContract:
    """Contract tests for webhook endpoints."""

    async def test_get_webhooks_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """GET /api/events/webhooks returns list."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            "/api/events/webhooks",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Webhooks require authentication")

        assert response.status_code == 200

        data = response.json()
        assert "data" in data
        assert isinstance(data["data"], list)

    async def test_create_webhook_response_schema(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """POST /api/events/webhooks returns created webhook."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "name": f"contract-test-{uuid4().hex[:8]}",
            "url": "https://httpbin.org/post",
            "secret": "test-secret",
            "event_types": ["hellarch.test.contract.v1"],
        }

        response = await gateway_client.post(
            "/api/events/webhooks",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Creating webhooks requires authentication")

        assert response.status_code == 201

        data = response.json()
        errors = WEBHOOK_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Cleanup
        webhook_id = data.get("id")
        if webhook_id:
            await gateway_client.request_delete(
                f"/api/events/webhooks/{webhook_id}",
                headers=headers,
            )


@pytest.mark.contract
class TestEventPublishContract:
    """Contract tests for event publishing."""

    async def test_publish_accepts_cloudevent(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """POST /api/events/publish accepts CloudEvent format."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        cloudevent = {
            "specversion": "1.0",
            "type": "hellarch.test.contract.publish.v1",
            "source": "contract-test",
            "id": str(uuid4()),
            "data": {"test": True},
        }

        # Validate our payload
        errors = CLOUD_EVENT_SCHEMA.validate(cloudevent)
        assert not errors, f"Test payload invalid: {errors}"

        response = await gateway_client.post(
            "/api/events/publish",
            json=cloudevent,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing events requires authentication")

        assert response.status_code in (200, 202)

    async def test_publish_rejects_invalid_cloudevent(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """POST /api/events/publish rejects invalid CloudEvent."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        # Missing required 'type' field
        invalid_event = {
            "specversion": "1.0",
            "source": "contract-test",
            "id": str(uuid4()),
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=invalid_event,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing events requires authentication")

        assert response.status_code in (400, 422)
