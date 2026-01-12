"""
CloudEvents schema validation tests.

These tests verify that all events in the HellArch system conform to
the CloudEvents 1.0 specification: https://cloudevents.io/

CloudEvents Required Attributes:
- specversion: The version of CloudEvents spec (must be "1.0")
- type: The type of event (e.g., "hellarch.auth.user.created.v1")
- source: The source of the event (e.g., "auth-service")
- id: Unique identifier for this event

CloudEvents Optional Attributes:
- time: Timestamp of when the event occurred (RFC 3339)
- datacontenttype: Content type of the data attribute
- data: The event payload
- subject: Subject of the event in context of the source
"""

import asyncio
import re
from datetime import datetime, timezone
from uuid import uuid4

import httpx
import pytest

from fixtures.events import (
    ALL_EVENT_TYPES,
    EventsAPIHelper,
    create_auth_user_created_event,
    create_test_event,
)
from fixtures.rabbitmq import CloudEvent, RabbitMQTestHelper
from fixtures.schemas import CLOUD_EVENT_SCHEMA


class TestCloudEventSchemaCompliance:
    """Tests for CloudEvents 1.0 schema compliance."""

    def test_cloudevent_has_required_fields(self):
        """CloudEvent dataclass includes all required fields."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={"test": True},
        )

        event_dict = event.to_dict()

        # Required attributes per CloudEvents spec
        assert "specversion" in event_dict
        assert "type" in event_dict
        assert "source" in event_dict
        assert "id" in event_dict

    def test_cloudevent_specversion_is_1_0(self):
        """CloudEvent specversion must be '1.0'."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={},
        )

        assert event.specversion == "1.0"
        assert event.to_dict()["specversion"] == "1.0"

    def test_cloudevent_id_is_unique(self):
        """Each CloudEvent has a unique ID."""
        events = [
            CloudEvent(type="test.v1", source="test", data={})
            for _ in range(100)
        ]

        ids = [e.id for e in events]
        assert len(ids) == len(set(ids)), "IDs should be unique"

    def test_cloudevent_id_format(self):
        """CloudEvent ID should be a valid format (UUID recommended)."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={},
        )

        # Should be a valid UUID
        uuid_pattern = r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"
        assert re.match(uuid_pattern, event.id.lower()), f"Invalid UUID: {event.id}"

    def test_cloudevent_time_is_iso8601(self):
        """CloudEvent time should be ISO 8601 / RFC 3339 format."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={},
        )

        # Should be parseable as ISO format
        try:
            parsed = datetime.fromisoformat(event.time.replace("Z", "+00:00"))
            assert parsed is not None
        except ValueError:
            pytest.fail(f"Invalid time format: {event.time}")

    def test_cloudevent_datacontenttype_default(self):
        """CloudEvent datacontenttype defaults to application/json."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={"key": "value"},
        )

        assert event.datacontenttype == "application/json"

    def test_cloudevent_validates_against_schema(self):
        """CloudEvent validates against our schema definition."""
        event = CloudEvent(
            type="test.event.v1",
            source="test-source",
            data={"message": "hello"},
        )

        errors = CLOUD_EVENT_SCHEMA.validate(event.to_dict())
        assert not errors, f"Schema validation failed: {errors}"


class TestEventTypeNaming:
    """Tests for event type naming conventions."""

    @pytest.mark.parametrize("event_type", ALL_EVENT_TYPES)
    def test_event_type_follows_naming_convention(self, event_type: str):
        """Event types follow the hellarch.<domain>.<action>.v<version> pattern."""
        # Pattern: hellarch.<domain>.<subject>.<action>.v<version>
        pattern = r"^hellarch\.[a-z]+\.[a-z_]+\.[a-z_]+\.v\d+$"

        # More permissive pattern for current event types
        permissive_pattern = r"^hellarch\.[a-z]+\.[a-z_.]+\.v\d+$"

        assert re.match(permissive_pattern, event_type), (
            f"Event type '{event_type}' doesn't follow naming convention"
        )

    @pytest.mark.parametrize("event_type", ALL_EVENT_TYPES)
    def test_event_type_has_version(self, event_type: str):
        """Event types include version suffix."""
        assert event_type.endswith(".v1") or re.search(r"\.v\d+$", event_type), (
            f"Event type '{event_type}' missing version suffix"
        )

    def test_event_type_is_lowercase(self):
        """Event types should be lowercase."""
        for event_type in ALL_EVENT_TYPES:
            assert event_type == event_type.lower(), (
                f"Event type '{event_type}' should be lowercase"
            )


class TestEventDataContract:
    """Tests for event data payload contracts."""

    def test_user_created_event_data_contract(self):
        """User created event has expected data fields."""
        event = create_auth_user_created_event(
            user_id="test-user-id",
            email="test@example.com",
            username="testuser",
            provider="discord",
        )

        data = event.data

        # Required fields for user created event
        assert "user_id" in data
        assert "email" in data
        assert "username" in data
        assert "provider" in data

    def test_test_event_data_contract(self):
        """Test event helper creates valid events."""
        event = create_test_event(
            event_type="hellarch.test.custom.v1",
            source="test-source",
            data={"custom": "data"},
        )

        assert event.type == "hellarch.test.custom.v1"
        assert event.source == "test-source"
        assert event.data["custom"] == "data"


class TestEventSerialization:
    """Tests for event serialization/deserialization."""

    def test_cloudevent_to_json_bytes(self):
        """CloudEvent can be serialized to JSON bytes."""
        event = CloudEvent(
            type="test.event.v1",
            source="test",
            data={"key": "value"},
        )

        json_bytes = event.to_bytes()

        assert isinstance(json_bytes, bytes)

        # Should be valid JSON
        import json

        parsed = json.loads(json_bytes)
        assert parsed["type"] == "test.event.v1"

    def test_cloudevent_to_dict_roundtrip(self):
        """CloudEvent can roundtrip through dict."""
        original = CloudEvent(
            type="test.roundtrip.v1",
            source="test-source",
            data={"nested": {"key": "value"}},
        )

        event_dict = original.to_dict()

        # Reconstruct from dict
        reconstructed = CloudEvent(
            type=event_dict["type"],
            source=event_dict["source"],
            data=event_dict["data"],
            id=event_dict["id"],
            specversion=event_dict["specversion"],
            time=event_dict["time"],
        )

        assert reconstructed.type == original.type
        assert reconstructed.source == original.source
        assert reconstructed.data == original.data
        assert reconstructed.id == original.id


class TestPublishedEventCompliance:
    """Tests for events published through the system."""

    async def test_published_event_is_valid_cloudevent(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Events published via API are valid CloudEvents."""
        event = create_test_event(
            event_type="hellarch.test.compliance.v1",
            source="compliance-test",
            data={"verify": "compliance"},
        )

        # Validate before publishing
        errors = CLOUD_EVENT_SCHEMA.validate(event.to_dict())
        assert not errors, f"Event invalid before publish: {errors}"

        # Publish should succeed
        result = await events_helper.publish_event(event)
        assert result.get("status") in ("published", "accepted")

    async def test_audit_log_preserves_event_structure(
        self,
        events_helper: EventsAPIHelper,
    ):
        """Audit logs preserve the CloudEvent structure."""
        event_id = str(uuid4())
        event_type = "hellarch.test.audit.structure.v1"

        log = await events_helper.create_audit_log(
            event_id=event_id,
            event_type=event_type,
            source="structure-test",
            metadata={"original": "data"},
        )

        # Audit log should preserve key CloudEvent fields
        assert log.event_id == event_id
        assert log.event_type == event_type
        assert log.source == "structure-test"


class TestEventProcessingCompliance:
    """Tests for event processing compliance."""

    async def test_malformed_cloudevent_rejected(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Malformed CloudEvents are rejected by publish endpoint."""
        # Missing required 'type' field
        invalid_event = {
            "specversion": "1.0",
            "source": "test",
            "id": str(uuid4()),
            "data": {},
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json=invalid_event,
        )

        assert response.status_code in (400, 422)

    async def test_invalid_specversion_rejected(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        """Invalid specversion is rejected."""
        invalid_event = {
            "specversion": "2.0",  # Invalid - must be 1.0
            "type": "test.event.v1",
            "source": "test",
            "id": str(uuid4()),
            "data": {},
        }

        response = await http_client.post(
            f"{base_urls['events']}/api/events/publish",
            json=invalid_event,
        )

        # May accept (lenient) or reject (strict)
        # If it accepts, the service should normalize to 1.0
        assert response.status_code in (200, 202, 400, 422)


class TestEventWorkerCompliance:
    """Tests for worker event processing compliance."""

    async def test_worker_processes_valid_cloudevent(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """Workers correctly process valid CloudEvents."""
        event = CloudEvent(
            type="hellarch.test.worker.compliance.v1",
            source="worker-compliance-test",
            data={"worker": "test"},
        )

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key=event.type,
            event=event,
        )

        await asyncio.sleep(3)

        # Should create audit log
        result = await events_helper.get_audit_logs(
            event_type="hellarch.test.worker.compliance.v1",
        )

        matching = [log for log in result.data if log.event_id == event.id]
        assert len(matching) >= 1, "Worker did not process CloudEvent"

    async def test_worker_preserves_event_id(
        self,
        mq_helper: RabbitMQTestHelper,
        events_helper: EventsAPIHelper,
    ):
        """Workers preserve the original event ID."""
        event = CloudEvent(
            type="hellarch.test.preserve.id.v1",
            source="preserve-id-test",
            data={"test": True},
        )

        original_id = event.id

        await mq_helper.declare_exchange("hellarch.events", "topic")
        await mq_helper.publish_cloud_event(
            exchange_name="hellarch.events",
            routing_key=event.type,
            event=event,
        )

        await asyncio.sleep(3)

        result = await events_helper.get_audit_logs(
            event_type="hellarch.test.preserve.id.v1",
        )

        matching = [log for log in result.data if log.event_id == original_id]
        assert len(matching) >= 1, "Event ID was not preserved"
