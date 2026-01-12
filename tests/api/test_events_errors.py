"""
Error path tests for the Events API service.

These tests verify proper error handling for edge cases and invalid inputs.
"""

from uuid import uuid4

import httpx
import pytest

from fixtures.events import EventsAPIHelper


class TestAuditLogErrorPaths:
    """Error scenarios for audit log endpoints."""

    async def test_create_audit_log_missing_required_fields(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating audit log without required fields returns error."""
        # Missing event_type
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={"event_id": str(uuid4()), "source": "test"},
        )
        assert response.status_code in (400, 422)

        # Missing event_id
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={"event_type": "test.event.v1", "source": "test"},
        )
        assert response.status_code in (400, 422)

        # Missing source
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={"event_id": str(uuid4()), "event_type": "test.event.v1"},
        )
        assert response.status_code in (400, 422)

    async def test_create_audit_log_empty_body(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating audit log with empty body returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={},
        )
        assert response.status_code in (400, 422)

    async def test_create_audit_log_malformed_json(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating audit log with malformed JSON returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            content=b"not valid json",
            headers={"Content-Type": "application/json"},
        )
        assert response.status_code in (400, 422)

    async def test_create_audit_log_invalid_uuid_format(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating audit log with invalid UUID format."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={
                "event_id": "not-a-uuid",
                "event_type": "test.event.v1",
                "source": "test",
            },
        )
        # Service may accept non-UUID event_ids or reject them
        assert response.status_code in (201, 400, 422)

    async def test_create_audit_log_invalid_metadata_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating audit log with non-object metadata."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            json={
                "event_id": str(uuid4()),
                "event_type": "test.event.v1",
                "source": "test",
                "metadata": "not an object",
            },
        )
        assert response.status_code in (400, 422)

    async def test_get_audit_log_nonexistent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Getting non-existent audit log returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs/{fake_id}"
        )
        assert response.status_code == 404

    async def test_get_audit_logs_invalid_pagination(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Invalid pagination parameters."""
        # Negative page
        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs",
            params={"page": -1},
        )
        assert response.status_code in (200, 400, 422)

        # Zero page size
        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs",
            params={"page_size": 0},
        )
        assert response.status_code in (200, 400, 422)

        # Extremely large page size
        response = await http_client.get(
            f"{base_urls['events']}/api/audit-logs",
            params={"page_size": 10000},
        )
        # Should either cap the size or reject
        assert response.status_code in (200, 400, 422)


class TestWebhookErrorPaths:
    """Error scenarios for webhook endpoints."""

    async def test_create_webhook_missing_required_fields(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating webhook without required fields returns error."""
        # Missing name
        response = await http_client.post(
            f"{base_urls['events']}/api/webhooks",
            json={
                "url": "https://example.com/webhook",
                "secret": "test-secret",
                "event_types": ["test.event.v1"],
            },
        )
        assert response.status_code in (400, 422)

        # Missing URL
        response = await http_client.post(
            f"{base_urls['events']}/api/webhooks",
            json={
                "name": "test-webhook",
                "secret": "test-secret",
                "event_types": ["test.event.v1"],
            },
        )
        assert response.status_code in (400, 422)

    async def test_create_webhook_invalid_url(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating webhook with invalid URL returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/webhooks",
            json={
                "name": "test-webhook",
                "url": "not-a-valid-url",
                "secret": "test-secret",
                "event_types": ["test.event.v1"],
            },
        )
        assert response.status_code in (400, 422)

    async def test_create_webhook_empty_event_types(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating webhook with empty event types list."""
        response = await http_client.post(
            f"{base_urls['events']}/api/webhooks",
            json={
                "name": "test-webhook",
                "url": "https://example.com/webhook",
                "secret": "test-secret",
                "event_types": [],
            },
        )
        # May be allowed or rejected depending on business rules
        assert response.status_code in (201, 400, 422)

    async def test_get_webhook_nonexistent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Getting non-existent webhook returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{base_urls['events']}/api/webhooks/{fake_id}"
        )
        assert response.status_code == 404

    async def test_update_webhook_nonexistent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Updating non-existent webhook returns 404."""
        fake_id = str(uuid4())
        response = await http_client.put(
            f"{base_urls['events']}/api/webhooks/{fake_id}",
            json={"name": "updated-name"},
        )
        assert response.status_code == 404

    async def test_delete_webhook_nonexistent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Deleting non-existent webhook returns 404 or 204."""
        fake_id = str(uuid4())
        response = await http_client.delete(
            f"{base_urls['events']}/api/webhooks/{fake_id}"
        )
        # Some APIs return 404, some return 204 (idempotent delete)
        assert response.status_code in (204, 404)

    async def test_update_webhook_invalid_url(
        self,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Updating webhook with invalid URL returns error."""
        # Create a valid webhook first
        webhook = await events_helper.create_webhook(
            name="test-update-invalid",
            url="https://example.com/valid",
            secret="test-secret",
            event_types=["test.event.v1"],
        )

        try:
            response = await http_client.put(
                f"{base_urls['events']}/api/webhooks/{webhook.id}",
                json={"url": "not-a-valid-url"},
            )
            assert response.status_code in (400, 422)
        finally:
            await events_helper.delete_webhook(webhook.id)


class TestWebhookDeliveryErrorPaths:
    """Error scenarios for webhook delivery endpoints."""

    async def test_create_delivery_missing_webhook_id(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating delivery without webhook_id returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/webhook-deliveries",
            json={
                "event_id": str(uuid4()),
                "event_type": "test.event.v1",
                "status_code": 200,
                "success": True,
            },
        )
        assert response.status_code in (400, 422)

    async def test_create_delivery_nonexistent_webhook(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Creating delivery for non-existent webhook."""
        fake_webhook_id = str(uuid4())
        response = await http_client.post(
            f"{base_urls['events']}/api/webhook-deliveries",
            json={
                "webhook_id": fake_webhook_id,
                "event_id": str(uuid4()),
                "event_type": "test.event.v1",
                "status_code": 200,
                "success": True,
            },
        )
        # May return 201 (foreign key not enforced) or 400/404
        assert response.status_code in (201, 400, 404, 422)

    async def test_get_delivery_nonexistent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Getting non-existent delivery returns 404."""
        fake_id = str(uuid4())
        response = await http_client.get(
            f"{base_urls['events']}/api/webhook-deliveries/{fake_id}"
        )
        assert response.status_code == 404


class TestEventPublishErrorPaths:
    """Error scenarios for event publishing."""

    async def test_publish_event_missing_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Publishing event without type returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json={
                "specversion": "1.0",
                "source": "test",
                "id": str(uuid4()),
                "data": {"test": True},
            },
        )
        assert response.status_code in (400, 422)

    async def test_publish_event_missing_source(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Publishing event without source returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json={
                "specversion": "1.0",
                "type": "test.event.v1",
                "id": str(uuid4()),
                "data": {"test": True},
            },
        )
        assert response.status_code in (400, 422)

    async def test_publish_event_empty_body(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Publishing event with empty body returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json={},
        )
        assert response.status_code in (400, 422)

    async def test_publish_event_malformed_cloudevent(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Publishing malformed CloudEvent returns error."""
        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json={
                "not": "a",
                "valid": "cloudevent",
            },
        )
        assert response.status_code in (400, 422)


class TestContentTypeHandling:
    """Content-Type header handling tests."""

    async def test_wrong_content_type_for_json(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Wrong content-type for JSON body."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            content=b'{"event_id": "test", "event_type": "test.v1", "source": "test"}',
            headers={"Content-Type": "text/plain"},
        )
        # May parse anyway or reject
        assert response.status_code in (201, 400, 415)

    async def test_missing_content_type(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Missing content-type header."""
        response = await http_client.post(
            f"{base_urls['events']}/api/audit-logs",
            content=b'{"event_id": "test", "event_type": "test.v1", "source": "test"}',
        )
        # Most APIs will default to application/json
        assert response.status_code in (201, 400, 415)
