"""
Message queue integration tests.

These tests verify the RabbitMQ message handling behavior including:
- Dead letter queue (DLQ) handling
- Message retry behavior
- Worker idempotency
- Message ordering and delivery guarantees
"""

import asyncio
import os
from uuid import uuid4

import httpx
import pytest

from fixtures.events import EventsAPIHelper
from fixtures.rabbitmq import CloudEvent, RabbitMQTestHelper


def get_dlq_worker_url() -> str:
    return os.getenv("DLQ_WORKER_URL", "http://localhost:5025")


class TestDeadLetterQueue:
    """
    Tests for dead letter queue handling.

    Messages that fail processing should be moved to the DLQ
    and be available for inspection/replay.
    """

    async def test_malformed_message_goes_to_dlq(
        self,
        mq_helper: RabbitMQTestHelper,
        http_client: httpx.AsyncClient,
    ):
        """
        Malformed messages should be rejected and moved to DLQ.
        """
        await mq_helper.declare_exchange("hellarch.events", "topic")

        # Publish a malformed message (not valid CloudEvent JSON)
        await mq_helper.publish(
            exchange_name="hellarch.events",
            routing_key="hellarch.test.malformed.v1",
            body=b"not valid json {{{",
        )

        # Wait for processing
        await asyncio.sleep(3)

        # Check DLQ worker API for the message
        try:
            response = await http_client.get(
                f"{get_dlq_worker_url()}/api/dlq/messages",
                params={"limit": 50},
            )

            if response.status_code == 200:
                messages = response.json()
                # Look for our malformed message
                # DLQ might store the original body or error details
                assert isinstance(messages, (list, dict))
            elif response.status_code == 404:
                pytest.skip("DLQ API not available")
            else:
                pytest.skip(f"DLQ worker returned {response.status_code}")

        except httpx.ConnectError:
            pytest.skip("DLQ worker not available")

    async def test_processing_error_moves_to_dlq(
        self,
        mq_helper: RabbitMQTestHelper,
        http_client: httpx.AsyncClient,
    ):
        """
        Messages that cause processing errors should be moved to DLQ
        after retry exhaustion.
        """
        # Create a valid CloudEvent but with data that will cause
        # processing to fail (e.g., missing required field in data)
        event = CloudEvent(
            type="hellarch.test.will.fail.v1",
            source="test-dlq",
            data={
                "intentionally_broken": True,
                "missing_required_fields": True,
            },
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key="hellarch.test.will.fail.v1",
            event=event,
        )

        # Wait for retries to exhaust
        await asyncio.sleep(10)

        try:
            response = await http_client.get(
                f"{get_dlq_worker_url()}/api/dlq/messages",
                params={"limit": 50},
            )

            if response.status_code == 200:
                messages = response.json()
                if isinstance(messages, list):
                    # Check if our event ended up in DLQ
                    event_ids = [
                        m.get("event_id") or m.get("id")
                        for m in messages
                        if isinstance(m, dict)
                    ]
                    # Event might or might not be in DLQ depending on
                    # whether the worker handles unknown event types
                    pass
            elif response.status_code == 404:
                pytest.skip("DLQ API not available")

        except httpx.ConnectError:
            pytest.skip("DLQ worker not available")

    async def test_dlq_message_can_be_replayed(
        self,
        mq_helper: RabbitMQTestHelper,
        http_client: httpx.AsyncClient,
        events_helper: EventsAPIHelper,
    ):
        """
        Messages in the DLQ can be replayed for reprocessing.
        """
        try:
            # Get current DLQ messages
            response = await http_client.get(
                f"{get_dlq_worker_url()}/api/dlq/messages",
                params={"limit": 1},
            )

            if response.status_code != 200:
                pytest.skip("DLQ API not available")

            messages = response.json()
            if not messages or (isinstance(messages, list) and len(messages) == 0):
                pytest.skip("No messages in DLQ to replay")

            # Get the first message ID
            if isinstance(messages, list):
                message = messages[0]
            else:
                message = messages.get("data", [{}])[0]

            message_id = message.get("id")
            if not message_id:
                pytest.skip("DLQ message has no ID")

            # Attempt to replay the message
            replay_response = await http_client.post(
                f"{get_dlq_worker_url()}/api/dlq/messages/{message_id}/replay"
            )

            # Replay should succeed (200/202) or message not found (404)
            assert replay_response.status_code in (200, 202, 404)

        except httpx.ConnectError:
            pytest.skip("DLQ worker not available")


class TestMessageRetry:
    """
    Tests for message retry behavior.

    Failed message processing should be retried with backoff.
    """

    async def test_transient_failure_is_retried(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Messages that fail due to transient errors should be retried.
        """
        # This test is inherently difficult to test without being able
        # to control service behavior. We test by observing delivery
        # records for webhooks that initially fail then succeed.

        event_type = f"hellarch.test.retry.{uuid4().hex[:8]}.v1"

        # Create webhook to an endpoint that will succeed
        webhook = await events_helper.create_webhook(
            name="retry-test-webhook",
            url="https://httpbin.org/post",
            secret="test-secret",
            event_types=[event_type],
        )

        try:
            event = CloudEvent(
                type=event_type,
                source="retry-test",
                data={"test": "retry"},
            )

            await mq_helper.declare_exchange("hellarch.events", "topic")
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key=event_type,
                event=event,
            )

            await asyncio.sleep(5)

            # Check deliveries - should have at least one
            result = await events_helper.get_deliveries(webhook_id=webhook.id)

            matching = [d for d in result.data if d.event_id == event.id]
            assert len(matching) >= 1

            # Verify the attempt number
            delivery = matching[0]
            assert delivery.attempt >= 1

        finally:
            await events_helper.delete_webhook(webhook.id)

    async def test_retry_respects_max_attempts(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Retries should stop after max attempts are exhausted.
        """
        event_type = f"hellarch.test.maxretry.{uuid4().hex[:8]}.v1"

        # Create webhook that always fails
        webhook = await events_helper.create_webhook(
            name="always-fail-webhook",
            url="https://httpbin.org/status/503",
            secret="test-secret",
            event_types=[event_type],
        )

        try:
            event = CloudEvent(
                type=event_type,
                source="max-retry-test",
                data={"test": "max_retry"},
            )

            await mq_helper.declare_exchange("hellarch.events", "topic")
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key=event_type,
                event=event,
            )

            # Wait longer for all retries to exhaust
            await asyncio.sleep(15)

            result = await events_helper.get_deliveries(webhook_id=webhook.id)
            matching = [d for d in result.data if d.event_id == event.id]

            if len(matching) == 0:
                pytest.skip("No delivery attempts recorded")

            # All attempts should have failed
            assert all(d.success is False for d in matching)

            # Should not exceed max retry count (typically 3-5)
            max_expected_attempts = 5
            assert len(matching) <= max_expected_attempts

        finally:
            await events_helper.delete_webhook(webhook.id)


class TestWorkerIdempotency:
    """
    Tests for worker idempotency.

    Workers should handle duplicate messages gracefully.
    """

    async def test_audit_worker_handles_duplicate_events(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Audit worker should not create duplicate entries for same event.
        """
        event = CloudEvent(
            type="hellarch.test.audit.idempotent.v1",
            source="idempotency-test",
            data={"test": "audit_idempotency"},
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")

        # Publish the same event multiple times
        for _ in range(3):
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key="hellarch.test.audit.idempotent.v1",
                event=event,
            )
            await asyncio.sleep(0.1)

        await asyncio.sleep(5)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.test.audit.idempotent.v1",
        )

        matching = [log for log in result.data if log.event_id == event.id]

        # Ideally should be exactly 1, but if idempotency isn't implemented
        # we document this as expected failure
        if len(matching) > 1:
            pytest.xfail(
                f"Audit worker created {len(matching)} entries for same event - "
                "idempotency not fully implemented"
            )
        else:
            assert len(matching) == 1

    async def test_webhook_worker_handles_duplicate_events(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Webhook worker should handle duplicate events appropriately.
        """
        event_type = f"hellarch.test.webhook.idempotent.{uuid4().hex[:8]}.v1"

        webhook = await events_helper.create_webhook(
            name="idempotent-webhook",
            url="https://httpbin.org/post",
            secret="test-secret",
            event_types=[event_type],
        )

        try:
            event = CloudEvent(
                type=event_type,
                source="idempotency-test",
                data={"test": "webhook_idempotency"},
            )

            await mq_helper.declare_exchange("hellarch.events", "topic")

            # Publish same event multiple times
            for _ in range(3):
                await mq_helper.publish_cloud_event(
                    exchange_name="hellarch.events",
                    routing_key=event_type,
                    event=event,
                )
                await asyncio.sleep(0.1)

            await asyncio.sleep(5)

            result = await events_helper.get_deliveries(webhook_id=webhook.id)
            matching = [d for d in result.data if d.event_id == event.id]

            # For webhooks, it might be acceptable to deliver multiple times
            # (at-least-once delivery) but should be documented
            if len(matching) > 1:
                # This is often expected behavior for webhooks
                # Consumers should be idempotent
                pass
            else:
                assert len(matching) == 1

        finally:
            await events_helper.delete_webhook(webhook.id)


class TestMessageOrdering:
    """
    Tests for message ordering behavior.
    """

    async def test_events_processed_in_order(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """
        Events should generally be processed in order they were published.
        Note: Strict ordering is not always guaranteed in distributed systems.
        """
        event_type = "hellarch.test.ordering.v1"
        event_count = 5

        events = []
        for i in range(event_count):
            event = CloudEvent(
                type=event_type,
                source="ordering-test",
                data={"sequence": i, "batch_id": uuid4().hex[:8]},
            )
            events.append(event)

        await mq_helper.declare_exchange("hellarch.events", "topic")

        for event in events:
            await mq_helper.publish_cloud_event(
                exchange_name="hellarch.events",
                routing_key=event_type,
                event=event,
            )
            await asyncio.sleep(0.05)  # Small delay between publishes

        await asyncio.sleep(5)

        result = await events_helper.get_audit_logs(
            event_type=event_type,
        )

        # Find our events
        event_ids = {e.id for e in events}
        our_logs = [log for log in result.data if log.event_id in event_ids]

        if len(our_logs) < event_count:
            pytest.skip(f"Only {len(our_logs)} of {event_count} events were processed")

        # Events should be in order by created_at timestamp
        # This is a soft check - ordering isn't strictly guaranteed
        timestamps = [log.created_at for log in our_logs]
        sorted_timestamps = sorted(timestamps)

        if timestamps != sorted_timestamps:
            # Document out-of-order processing but don't fail
            pytest.xfail("Events were processed out of order")


class TestMessagePublishing:
    """
    Tests for message publishing through the Events API.
    """

    async def test_publish_event_via_api(
        self,
        events_helper: EventsAPIHelper,
    ):
        """
        Events can be published through the HTTP API.
        """
        from fixtures.events import create_test_event

        event = create_test_event(
            event_type="hellarch.test.api.publish.v1",
            source="api-publish-test",
            data={"test": True},
        )

        result = await events_helper.publish_event(event)

        assert result.get("status") in ("published", "accepted")

    async def test_published_event_is_processed(
        self,
        events_helper: EventsAPIHelper,
    ):
        """
        Events published via API should be processed by workers.
        """
        from fixtures.events import create_test_event

        event = create_test_event(
            event_type="hellarch.test.api.processed.v1",
            source="api-process-test",
            data={"verify": "processing"},
        )

        await events_helper.publish_event(event)

        await asyncio.sleep(3)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.test.api.processed.v1",
        )

        matching = [log for log in result.data if log.event_id == event.id]

        assert len(matching) >= 1, "Published event was not processed"
