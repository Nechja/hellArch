"""Event publishing for test results."""

import httpx
import structlog

from testing.models import (
    CloudEvent,
    E2eTestOutcome,
    GoldenPathFailure,
    TestCategory,
    TestOutcome,
    TestResult,
    TestRunSummary,
)

logger = structlog.get_logger()


class TestEventPublisher:
    """Publishes test result events to the events service."""

    def __init__(self, events_url: str) -> None:
        self.events_url = events_url.rstrip("/")

    def _create_event(self, event_type: str, data: dict) -> CloudEvent:
        """Create a CloudEvent."""
        return CloudEvent(type=event_type, data=data)

    async def _publish(self, event: CloudEvent) -> bool:
        """Publish an event to the events service."""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{self.events_url}/publish",
                    json=event.model_dump(mode="json"),
                    timeout=5.0,
                )
                if response.status_code in (200, 201, 202):
                    logger.info("Published event", type=event.type)
                    return True
                logger.warning(
                    "Failed to publish event",
                    status=response.status_code,
                    type=event.type,
                )
        except httpx.HTTPError as e:
            logger.error("Error publishing event", error=str(e), type=event.type)
        return False

    async def publish_test_result(self, outcome: TestOutcome) -> bool:
        """Publish a test result event."""
        event_type = f"hellarch.test.{outcome.result.value}.v1"
        event = self._create_event(event_type, outcome.model_dump(mode="json"))
        return await self._publish(event)

    async def publish_golden_path_failure(self, failure: GoldenPathFailure) -> bool:
        """Publish a critical golden path failure event."""
        event = self._create_event(
            "hellarch.test.golden_path_failed.v1",
            failure.model_dump(mode="json"),
        )
        return await self._publish(event)

    async def publish_test_run_summary(self, summary: TestRunSummary) -> bool:
        """Publish a test run summary event."""
        event = self._create_event(
            "hellarch.test.run_completed.v1",
            summary.model_dump(mode="json"),
        )
        return await self._publish(event)

    async def publish_e2e_result(self, outcome: E2eTestOutcome) -> bool:
        """Publish an E2E test result event with screenshots."""
        event_type = f"hellarch.test.e2e.{outcome.result.value}.v1"
        event = self._create_event(event_type, outcome.model_dump(mode="json"))
        return await self._publish(event)

    async def publish_e2e_failure(self, outcome: E2eTestOutcome) -> bool:
        """Publish critical E2E failure event."""
        event = self._create_event(
            "hellarch.test.e2e.critical_failed.v1",
            outcome.model_dump(mode="json"),
        )
        return await self._publish(event)


async def publish_outcome(
    publisher: TestEventPublisher,
    test_name: str,
    category: TestCategory,
    result: TestResult,
    duration_ms: int,
    error: str | None = None,
    critical: bool = False,
) -> bool:
    """Helper to create and publish a test outcome."""
    outcome = TestOutcome(
        test_name=test_name,
        category=category,
        result=result,
        duration_ms=duration_ms,
        error=error,
        critical=critical,
    )
    return await publisher.publish_test_result(outcome)
