"""
Tests for the Events API service.

These tests are designed to work against both live systems and test rigs.
Set EVENTS_URL environment variable to point to the target system.
"""

from uuid import uuid4

import httpx
import pytest
from fixtures.events import (
    ALL_EVENT_TYPES,
    EventsAPIHelper,
    create_auth_user_created_event,
    create_infra_service_unhealthy_event,
    create_test_event,
)


class TestEventsAPIHealth:
    """Health check tests for the Events API."""

    async def test_health_check(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Verify the Events API is healthy."""
        response = await http_client.get(f"{base_urls['events']}/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"


class TestAuditLogs:
    """Tests for audit log endpoints."""

    async def test_get_audit_logs_empty(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Get audit logs returns paginated response."""
        result = await events_helper.get_audit_logs()

        assert result.page == 1
        assert result.page_size == 20
        assert isinstance(result.data, list)
        assert isinstance(result.total_count, int)

    async def test_create_and_get_audit_log(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Create an audit log and retrieve it."""
        event_id = str(uuid4())
        actor_id = str(uuid4())
        target_id = str(uuid4())

        audit_log = await events_helper.create_audit_log(
            event_id=event_id,
            event_type="hellarch.auth.user.created.v1",
            source="test-suite",
            actor_id=actor_id,
            target_id=target_id,
            metadata={"test": True, "reason": "automated test"},
        )

        assert audit_log.event_id == event_id
        assert audit_log.event_type == "hellarch.auth.user.created.v1"
        assert audit_log.source == "test-suite"
        assert audit_log.actor_id == actor_id
        assert audit_log.target_id == target_id
        assert audit_log.metadata is not None
        assert audit_log.metadata["test"] is True

        fetched = await events_helper.get_audit_log(audit_log.id)
        assert fetched.id == audit_log.id
        assert fetched.event_id == event_id

    async def test_filter_audit_logs_by_event_type(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Filter audit logs by event type."""
        event_type = "hellarch.auth.user.created.v1"
        await events_helper.create_audit_log(
            event_id=str(uuid4()),
            event_type=event_type,
            source="test-suite",
        )

        result = await events_helper.get_audit_logs(event_type=event_type)

        assert all(log.event_type == event_type for log in result.data)

    async def test_filter_audit_logs_by_actor(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Filter audit logs by actor ID."""
        actor_id = str(uuid4())
        await events_helper.create_audit_log(
            event_id=str(uuid4()),
            event_type="hellarch.test.event.v1",
            source="test-suite",
            actor_id=actor_id,
        )

        result = await events_helper.get_audit_logs(actor_id=actor_id)

        assert len(result.data) >= 1
        assert all(log.actor_id == actor_id for log in result.data)

    async def test_audit_log_pagination(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Test pagination of audit logs."""
        for i in range(5):
            await events_helper.create_audit_log(
                event_id=str(uuid4()),
                event_type="hellarch.test.pagination.v1",
                source="test-suite",
                metadata={"index": i},
            )

        page1 = await events_helper.get_audit_logs(page=1, page_size=2)
        page2 = await events_helper.get_audit_logs(page=2, page_size=2)

        assert page1.page == 1
        assert page2.page == 2
        assert page1.page_size == 2
        assert len(page1.data) <= 2
        assert len(page2.data) <= 2


class TestWebhooks:
    """Tests for webhook endpoints."""

    async def test_get_webhooks_empty(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Get webhooks returns paginated response."""
        result = await events_helper.get_webhooks()

        assert result.page == 1
        assert isinstance(result.data, list)

    async def test_create_webhook(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Create a webhook."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/test-webhook",
            event_types=["hellarch.auth.user.created.v1", "hellarch.auth.user.updated.v1"],
            secret="test-secret-123",  # noqa: S106
            is_active=True,
        )

        try:
            assert webhook.url == "https://example.com/test-webhook"
            assert "hellarch.auth.user.created.v1" in webhook.event_types
            assert webhook.is_active is True
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_update_webhook(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Update an existing webhook."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/original",
            event_types=["hellarch.auth.user.created.v1"],
        )

        try:
            updated = await events_helper.update_webhook(
                webhook_id=webhook.id,
                url="https://example.com/updated",
                is_active=False,
            )

            assert updated.url == "https://example.com/updated"
            assert updated.is_active is False
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_delete_webhook(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Delete a webhook."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/to-delete",
            event_types=["hellarch.test.event.v1"],
        )

        await events_helper.delete_webhook(webhook.id)

        with pytest.raises(httpx.HTTPStatusError) as exc_info:
            await events_helper.get_webhook(webhook.id)
        assert exc_info.value.response.status_code == 404

    async def test_get_webhooks_by_event_type(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Get webhooks subscribed to a specific event type."""
        event_type = "hellarch.test.specific.v1"
        webhook = await events_helper.create_webhook(
            url="https://example.com/specific-webhook",
            event_types=[event_type],
            is_active=True,
        )

        try:
            webhooks = await events_helper.get_webhooks_by_event_type(event_type)
            webhook_ids = [w.id for w in webhooks]
            assert webhook.id in webhook_ids
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_inactive_webhook_not_returned_by_event_type(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Inactive webhooks are not returned when querying by event type."""
        event_type = "hellarch.test.inactive.v1"
        webhook = await events_helper.create_webhook(
            url="https://example.com/inactive-webhook",
            event_types=[event_type],
            is_active=False,
        )

        try:
            webhooks = await events_helper.get_webhooks_by_event_type(event_type)
            webhook_ids = [w.id for w in webhooks]
            assert webhook.id not in webhook_ids
        finally:
            await events_helper.delete_webhook(webhook.id)


class TestWebhookDeliveries:
    """Tests for webhook delivery endpoints."""

    async def test_get_deliveries_empty(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Get deliveries returns paginated response."""
        result = await events_helper.get_deliveries()

        assert result.page == 1
        assert isinstance(result.data, list)

    async def test_create_and_get_delivery(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Create a delivery record and retrieve it."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/delivery-test",
            event_types=["hellarch.test.event.v1"],
        )

        try:
            delivery = await events_helper.create_delivery(
                webhook_id=webhook.id,
                event_id=str(uuid4()),
                status_code=200,
                response_body='{"ok": true}',
                success=True,
            )

            assert delivery.webhook_id == webhook.id
            assert delivery.status_code == 200
            assert delivery.success is True

            fetched = await events_helper.get_delivery(delivery.id)
            assert fetched.id == delivery.id
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_filter_deliveries_by_webhook(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Filter deliveries by webhook ID."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/filter-test",
            event_types=["hellarch.test.event.v1"],
        )

        try:
            for _ in range(3):
                await events_helper.create_delivery(
                    webhook_id=webhook.id,
                    event_id=str(uuid4()),
                    status_code=200,
                    success=True,
                )

            result = await events_helper.get_deliveries(webhook_id=webhook.id)
            assert len(result.data) >= 3
            assert all(d.webhook_id == webhook.id for d in result.data)
        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_failed_delivery_record(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Record a failed delivery attempt."""
        webhook = await events_helper.create_webhook(
            url="https://example.com/failed-test",
            event_types=["hellarch.test.event.v1"],
        )

        try:
            delivery = await events_helper.create_delivery(
                webhook_id=webhook.id,
                event_id=str(uuid4()),
                status_code=500,
                response_body="Internal Server Error",
                error="Server returned 500",
                success=False,
            )

            assert delivery.success is False
            assert delivery.status_code == 500
            assert delivery.error == "Server returned 500"
        finally:
            await events_helper.delete_webhook(webhook.id)


class TestEventPublishing:
    """Tests for event publishing endpoint."""

    async def test_publish_event(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Publish an event through the API."""
        event = create_test_event(
            event_type="hellarch.test.publish.v1",
            data={"message": "test event"},
        )

        result = await events_helper.publish_event(event)

        assert result.get("status") in ["published", "accepted"]

    async def test_publish_user_created_event(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Publish a user created event."""
        event = create_auth_user_created_event(
            user_id=str(uuid4()),
            email="test@example.com",
            username="testuser",
            provider="discord",
        )

        result = await events_helper.publish_event(event)

        assert result.get("status") in ["published", "accepted"]

    async def test_publish_infra_event(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Publish an infrastructure event."""
        event = create_infra_service_unhealthy_event(
            service_name="test-service",
            reason="Connection timeout",
            details={"timeout_ms": 5000},
        )

        result = await events_helper.publish_event(event)

        assert result.get("status") in ["published", "accepted"]


class TestEventTypes:
    """Tests related to event type handling."""

    @pytest.mark.parametrize("event_type", ALL_EVENT_TYPES)
    async def test_create_audit_log_for_each_event_type(
        self,
        events_helper: EventsAPIHelper,
        event_type: str,
    ):
        """Verify all known event types can be logged."""
        audit_log = await events_helper.create_audit_log(
            event_id=str(uuid4()),
            event_type=event_type,
            source="test-suite",
        )

        assert audit_log.event_type == event_type


class TestGatewayAccess:
    """Tests for accessing Events API through the Gateway."""

    async def test_audit_logs_via_gateway(
        self,
        events_helper_via_gateway: EventsAPIHelper,
    ):
        """Access audit logs through the gateway."""
        try:
            result = await events_helper_via_gateway.get_audit_logs()
            assert isinstance(result.data, list)
        except httpx.HTTPStatusError as e:
            if e.response.status_code == 401:
                pytest.skip("Gateway requires authentication for this endpoint")
            raise

    async def test_webhooks_via_gateway(
        self,
        events_helper_via_gateway: EventsAPIHelper,
    ):
        """Access webhooks through the gateway."""
        try:
            result = await events_helper_via_gateway.get_webhooks()
            assert isinstance(result.data, list)
        except httpx.HTTPStatusError as e:
            if e.response.status_code == 401:
                pytest.skip("Gateway requires authentication for this endpoint")
            raise
