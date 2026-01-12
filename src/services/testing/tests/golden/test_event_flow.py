"""Golden path test: Event flow."""

import uuid

import pytest

from testing.clients import EventsClient
from testing.models import CloudEvent


@pytest.mark.golden
async def test_event_publish_and_retrieve(events_client: EventsClient) -> None:
    """Complete event publish and retrieval cycle."""
    # Create a unique test event
    test_id = str(uuid.uuid4())
    test_event = CloudEvent(
        type="hellarch.test.golden_path.v1",
        data={
            "test_id": test_id,
            "message": "Golden path test event",
        },
    )

    # Step 1: Publish event
    published = await events_client.publish(test_event.model_dump(mode="json"))
    assert published, "Failed to publish event"

    # Step 2: Small delay for event to be stored
    import asyncio
    await asyncio.sleep(0.5)

    # Step 3: Retrieve events and verify our event exists
    events = await events_client.get_events(
        event_type="hellarch.test.golden_path.v1",
        limit=20,
    )

    # Look for our specific event
    found = any(
        e.data.get("test_id") == test_id
        for e in events
    )
    assert found, f"Published event with test_id {test_id} not found in retrieved events"


@pytest.mark.golden
class TestEventFlow:
    """Event flow tests as separate steps."""

    _test_id: str | None = None

    async def test_publish_event(self, events_client: EventsClient) -> None:
        """Step 1: Publish a test event."""
        TestEventFlow._test_id = str(uuid.uuid4())
        event = CloudEvent(
            type="hellarch.test.step.v1",
            data={"test_id": self._test_id},
        )

        result = await events_client.publish(event.model_dump(mode="json"))
        assert result, "Event publish failed"

    async def test_event_stored(self, events_client: EventsClient) -> None:
        """Step 2: Verify event was stored."""
        if not self._test_id:
            pytest.skip("No test_id from publish step")

        import asyncio
        await asyncio.sleep(0.5)

        events = await events_client.get_events(
            event_type="hellarch.test.step.v1",
            limit=50,
        )

        found = any(e.data.get("test_id") == self._test_id for e in events)
        assert found, "Event not found in storage"
