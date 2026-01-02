"""
Integration tests for the complete events flow.

These tests verify the end-to-end flow:
RabbitMQ → Workers → Events API → Audit Logs

These tests work against both live systems and test rigs.
"""

import asyncio
from uuid import uuid4

import httpx
from fixtures.events import (
    EventsAPIHelper,
    create_auth_session_created_event,
    create_auth_user_created_event,
    create_infra_service_unhealthy_event,
)
from fixtures.rabbitmq import RabbitMQTestHelper


class TestAuditWorkerFlow:
    """Tests for the audit worker event processing flow."""

    async def test_user_created_event_creates_audit_log(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Verify that a user created event published to RabbitMQ
        results in an audit log entry in the Events API.
        """
        user_id = str(uuid4())
        event = create_auth_user_created_event(
            user_id=user_id,
            email="integration-test@example.com",
            username="integrationtest",
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key="hellarch.auth.user.created.v1",
            event=event,
        )

        await asyncio.sleep(2)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.auth.user.created.v1",
        )

        matching_logs = [log for log in result.data if log.event_id == event.id]
        assert len(matching_logs) >= 1, f"Expected audit log for event {event.id}"

        audit_log = matching_logs[0]
        assert audit_log.event_type == "hellarch.auth.user.created.v1"
        assert audit_log.source == "auth-service"

    async def test_session_created_event_flow(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """Verify session created events are properly audited."""
        session_id = str(uuid4())
        user_id = str(uuid4())
        event = create_auth_session_created_event(
            session_id=session_id,
            user_id=user_id,
            ip_address="192.168.1.100",
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key="hellarch.auth.session.created.v1",
            event=event,
        )

        await asyncio.sleep(2)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.auth.session.created.v1",
        )

        matching_logs = [log for log in result.data if log.event_id == event.id]
        assert len(matching_logs) >= 1

    async def test_infra_event_flow(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """Verify infrastructure events are properly audited."""
        event = create_infra_service_unhealthy_event(
            service_name="test-integration-service",
            reason="Integration test failure simulation",
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key="hellarch.infra.service.unhealthy.v1",
            event=event,
        )

        await asyncio.sleep(2)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.infra.service.unhealthy.v1",
        )

        matching_logs = [log for log in result.data if log.event_id == event.id]
        assert len(matching_logs) >= 1


class TestWebhookWorkerFlow:
    """Tests for the webhook worker event processing flow."""

    async def test_webhook_receives_event(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
        http_client: httpx.AsyncClient,
    ):
        """
        Verify that when a webhook is configured for an event type,
        a delivery record is created when that event is published.

        Note: This test requires a webhook endpoint that can receive events.
        In practice, you'd use a mock server or httpbin.org.
        """
        event_type = "hellarch.test.webhook.flow.v1"

        webhook = await events_helper.create_webhook(
            url="https://httpbin.org/post",
            event_types=[event_type],
            is_active=True,
        )

        try:
            from fixtures.rabbitmq import CloudEvent

            event = CloudEvent(
                type=event_type,
                source="integration-test",
                data={"test": True, "webhook_id": webhook.id},
            )

            await mq_helper.declare_exchange("hellarch.events", "topic")
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key=event_type,
                event=event,
            )

            await asyncio.sleep(5)

            result = await events_helper.get_deliveries(webhook_id=webhook.id)

            matching_deliveries = [
                d for d in result.data if d.event_id == event.id
            ]
            assert len(matching_deliveries) >= 1, (
                f"Expected delivery for event {event.id} to webhook {webhook.id}"
            )

        finally:
            await events_helper.delete_webhook(webhook.id)


class TestRealtimeWorkerFlow:
    """Tests for the realtime worker SignalR broadcasting."""

    async def test_event_is_broadcast_via_signalr(
        self,
        mq_helper: RabbitMQTestHelper,
        base_urls: dict[str, str],
    ):
        """
        Verify that events are broadcast via SignalR.

        Note: Full SignalR client testing would require additional
        SignalR client library. This is a placeholder for manual
        verification or more sophisticated tooling.
        """
        pass


class TestEventPublishingFlow:
    """Tests for publishing events through the Events API."""

    async def test_published_event_is_audited(
        self,
        events_helper: EventsAPIHelper,
    ):
        """
        Verify that events published through the Events API
        are processed by the audit worker and logged.
        """
        from fixtures.events import create_test_event

        event = create_test_event(
            event_type="hellarch.test.publish.audit.v1",
            source="integration-test",
            data={"verification": "audit_flow"},
        )

        await events_helper.publish_event(event)

        await asyncio.sleep(3)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.test.publish.audit.v1",
        )

        matching_logs = [log for log in result.data if log.event_id == event.id]
        assert len(matching_logs) >= 1, (
            f"Expected audit log for published event {event.id}"
        )


class TestEndToEndEventFlow:
    """Complete end-to-end tests covering multiple components."""

    async def test_complete_user_lifecycle_events(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Test the complete user lifecycle through events:
        1. User created
        2. Session created
        3. Session ended
        4. User deleted
        """
        from fixtures.events import (
            create_auth_session_ended_event,
            create_auth_user_deleted_event,
        )

        user_id = str(uuid4())
        session_id = str(uuid4())

        events = [
            (
                "hellarch.auth.user.created.v1",
                create_auth_user_created_event(
                    user_id=user_id,
                    email="lifecycle@example.com",
                    username="lifecycleuser",
                ),
            ),
            (
                "hellarch.auth.session.created.v1",
                create_auth_session_created_event(
                    session_id=session_id,
                    user_id=user_id,
                ),
            ),
            (
                "hellarch.auth.session.ended.v1",
                create_auth_session_ended_event(
                    session_id=session_id,
                    user_id=user_id,
                    reason="test_logout",
                ),
            ),
            (
                "hellarch.auth.user.deleted.v1",
                create_auth_user_deleted_event(user_id=user_id),
            ),
        ]

        await mq_helper.declare_exchange("hellarch.events", "topic")

        for routing_key, event in events:
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key=routing_key,
                event=event,
            )
            await asyncio.sleep(0.5)

        await asyncio.sleep(3)

        for routing_key, event in events:
            result = await events_helper.get_audit_logs(event_type=routing_key)
            matching = [log for log in result.data if log.event_id == event.id]
            assert len(matching) >= 1, (
                f"Missing audit log for {routing_key} event {event.id}"
            )
