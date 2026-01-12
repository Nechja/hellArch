"""
Contract tests for the Events service API.

These tests verify that API responses match expected schemas,
catching breaking changes in the API contract.
"""

from uuid import uuid4

import httpx
import pytest

from fixtures.events import EventsAPIHelper
from fixtures.schemas import (
    AUDIT_LOG_SCHEMA,
    AUDIT_LOGS_RESPONSE_SCHEMA,
    CLOUD_EVENT_SCHEMA,
    EVENTS_HEALTH_SCHEMA,
    WEBHOOK_DELIVERIES_RESPONSE_SCHEMA,
    WEBHOOK_DELIVERY_SCHEMA,
    WEBHOOK_SCHEMA,
    WEBHOOKS_RESPONSE_SCHEMA,
)


class TestEventsHealthContract:
    """Contract tests for /health endpoint."""

    async def test_health_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Health endpoint returns expected schema."""
        response = await http_client.get(f"{base_urls['events']}/health")

        assert response.status_code == 200

        data = response.json()
        errors = EVENTS_HEALTH_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        assert data["status"] in ("healthy", "ok", "up")


class TestAuditLogsContract:
    """Contract tests for audit log endpoints."""

    async def test_get_audit_logs_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """GET /audit-logs returns paginated response schema."""
        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs",
            params={"page": 1, "page_size": 10},
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUDIT_LOGS_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: data must be a list
        assert isinstance(data.get("data"), list)

    async def test_create_audit_log_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /audit-logs returns created log schema."""
        payload = {
            "event_id": str(uuid4()),
            "event_type": "hellarch.test.contract.v1",
            "source": "contract-test",
            "metadata": {"test": True},
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json=payload,
        )

        assert response.status_code == 201

        data = response.json()
        errors = AUDIT_LOG_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract: returned log must have ID
        assert "id" in data
        assert data["event_type"] == payload["event_type"]

    async def test_get_single_audit_log_response_schema(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """GET /audit-logs/{id} returns single log schema."""
        # Create a log first
        log = await events_helper.create_audit_log(
            event_id=str(uuid4()),
            event_type="hellarch.test.single.v1",
            source="contract-test",
        )

        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs/{log.id}"
        )

        assert response.status_code == 200

        data = response.json()
        errors = AUDIT_LOG_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

    async def test_audit_log_metadata_is_object(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Audit log metadata field is always an object or null."""
        log = await events_helper.create_audit_log(
            event_id=str(uuid4()),
            event_type="hellarch.test.metadata.v1",
            source="contract-test",
            metadata={"key": "value", "nested": {"a": 1}},
        )

        assert log.metadata is None or isinstance(log.metadata, dict)

    async def test_audit_logs_pagination_contract(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Pagination parameters work as expected."""
        # First page
        response1 = await http_client.get(
            f"{base_urls['events']}/api/audit-logs",
            params={"page": 1, "page_size": 5},
        )
        data1 = response1.json()

        # Verify pagination fields
        assert "data" in data1
        assert len(data1["data"]) <= 5  # Respects page_size


class TestWebhooksContract:
    """Contract tests for webhook endpoints."""

    async def test_get_webhooks_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """GET /webhooks returns paginated response schema."""
        response = await http_client.get(
            f"{base_urls['events']}/api/webhooks",
            params={"page": 1, "page_size": 10},
        )

        assert response.status_code == 200

        data = response.json()
        errors = WEBHOOKS_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

    async def test_create_webhook_response_schema(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /webhooks returns created webhook schema."""
        payload = {
            "name": "contract-test-webhook",
            "url": "https://example.com/contract-test",
            "secret": "test-secret-123",
            "event_types": ["hellarch.test.contract.v1"],
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/webhooks",
            json=payload,
        )

        assert response.status_code == 201

        data = response.json()
        errors = WEBHOOK_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

        # Contract assertions
        assert data["name"] == payload["name"]
        assert data["url"] == payload["url"]
        assert data["is_active"] is True  # Default to active

        # Cleanup
        await events_helper.delete_webhook(data["id"])

    async def test_get_single_webhook_response_schema(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """GET /webhooks/{id} returns single webhook schema."""
        webhook = await events_helper.create_webhook(
            name="single-contract-test",
            url="https://example.com/single",
            secret="test-secret",
            event_types=["hellarch.test.v1"],
        )

        try:
            response = await http_client.get(
                f"{base_urls['events']}/api/webhooks/{webhook.id}"
            )

            assert response.status_code == 200

            data = response.json()
            errors = WEBHOOK_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_update_webhook_response_schema(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """PUT /webhooks/{id} returns updated webhook schema."""
        webhook = await events_helper.create_webhook(
            name="update-contract-test",
            url="https://example.com/update",
            secret="test-secret",
            event_types=["hellarch.test.v1"],
        )

        try:
            response = await http_client.put(
                f"{base_urls['events']}/api/webhooks/{webhook.id}",
                json={"name": "updated-name"},
            )

            assert response.status_code == 200

            data = response.json()
            errors = WEBHOOK_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"

            assert data["name"] == "updated-name"
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_webhook_event_types_is_array(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Webhook event_types is always an array of strings."""
        webhook = await events_helper.create_webhook(
            name="event-types-test",
            url="https://example.com/types",
            secret="test-secret",
            event_types=["type1.v1", "type2.v1", "type3.v1"],
        )

        try:
            assert isinstance(webhook.event_types, list)
            assert all(isinstance(t, str) for t in webhook.event_types)
        finally:
            await events_helper.delete_webhook(webhook.id)


class TestWebhookDeliveriesContract:
    """Contract tests for webhook delivery endpoints."""

    async def test_get_deliveries_response_schema(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """GET /webhook-deliveries returns paginated response schema."""
        response = await http_client.get(
            f"{base_urls['events']}/api/webhook-deliveries",
            params={"page": 1, "page_size": 10},
        )

        assert response.status_code == 200

        data = response.json()
        errors = WEBHOOK_DELIVERIES_RESPONSE_SCHEMA.validate(data)
        assert not errors, f"Schema validation failed: {errors}"

    async def test_create_delivery_response_schema(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /webhook-deliveries returns created delivery schema."""
        # Create a webhook first
        webhook = await events_helper.create_webhook(
            name="delivery-contract-test",
            url="https://example.com/delivery",
            secret="test-secret",
            event_types=["hellarch.test.v1"],
        )

        try:
            payload = {
                "webhook_id": webhook.id,
                "event_id": str(uuid4()),
                "event_type": "hellarch.test.v1",
                "status_code": 200,
                "success": True,
                "attempt": 1,
            }

            response = await http_client.post(
                f"{base_urls['events']}/api/webhook-deliveries",
                json=payload,
            )

            assert response.status_code == 201

            data = response.json()
            errors = WEBHOOK_DELIVERY_SCHEMA.validate(data)
            assert not errors, f"Schema validation failed: {errors}"

            # Contract assertions
            assert data["webhook_id"] == webhook.id
            assert data["success"] is True
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_delivery_status_code_is_integer(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Delivery status_code is always an integer."""
        webhook = await events_helper.create_webhook(
            name="status-code-test",
            url="https://example.com/status",
            secret="test-secret",
            event_types=["hellarch.test.v1"],
        )

        try:
            delivery = await events_helper.create_delivery(
                webhook_id=webhook.id,
                event_id=str(uuid4()),
                event_type="hellarch.test.v1",
                status_code=503,
                success=False,
            )

            assert isinstance(delivery.status_code, int)
        finally:
            await events_helper.delete_webhook(webhook.id)


class TestEventPublishContract:
    """Contract tests for event publishing endpoint."""

    async def test_publish_event_accepts_cloudevent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /events/publish accepts CloudEvent format."""
        cloudevent = {
            "specversion": "1.0",
            "type": "hellarch.test.publish.contract.v1",
            "source": "contract-test",
            "id": str(uuid4()),
            "data": {"test": True},
        }

        # Validate our test payload matches CloudEvent schema
        errors = CLOUD_EVENT_SCHEMA.validate(cloudevent)
        assert not errors, f"Test payload invalid: {errors}"

        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json=cloudevent,
        )

        # Should accept the event
        assert response.status_code in (200, 202)

    async def test_publish_event_requires_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /events/publish requires type field."""
        invalid_event = {
            "specversion": "1.0",
            "source": "contract-test",
            "id": str(uuid4()),
            "data": {"test": True},
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json=invalid_event,
        )

        assert response.status_code in (400, 422)

    async def test_publish_event_requires_source(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """POST /events/publish requires source field."""
        invalid_event = {
            "specversion": "1.0",
            "type": "hellarch.test.v1",
            "id": str(uuid4()),
            "data": {"test": True},
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json=invalid_event,
        )

        assert response.status_code in (400, 422)


class TestEventsResponseHeaders:
    """Contract tests for response headers."""

    async def test_all_endpoints_return_json(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """All Events API endpoints return JSON content type."""
        endpoints = [
            ("GET", "/health"),
            ("GET", "/api/audit-logs"),
            ("GET", "/api/webhooks"),
            ("GET", "/api/webhook-deliveries"),
        ]

        for method, path in endpoints:
            if method == "GET":
                response = await http_client.get(f"{base_urls['events']}{path}")
            else:
                response = await http_client.post(f"{base_urls['events']}{path}")

            content_type = response.headers.get("content-type", "")
            assert "application/json" in content_type, (
                f"{method} {path} did not return JSON: {content_type}"
            )
