"""Main test runner for the testing service."""

import asyncio
import time
from datetime import UTC, datetime

import structlog

from testing.config import Settings, TestConfig, load_config
from testing.events import TestEventPublisher
from testing.models import (
    GoldenPathFailure,
    TestCategory,
    TestOutcome,
    TestResult,
    TestRunSummary,
)

logger = structlog.get_logger()


class TestRunner:
    """Manages test execution and scheduling."""

    def __init__(self, settings: Settings, config: TestConfig) -> None:
        self.settings = settings
        self.config = config
        self.publisher = TestEventPublisher(settings.events_url)
        self._running = False

    async def run_smoke_tests(self) -> TestRunSummary:
        """Run all smoke tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.smoke)

        for test in self.config.tests.smoke:
            test_start = time.monotonic()
            try:
                result = await self._run_smoke_test(test.name, test.target)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if result:
                    passed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.SMOKE,
                        result=TestResult.PASSED,
                        duration_ms=duration_ms,
                    )
                else:
                    failed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.SMOKE,
                        result=TestResult.FAILED,
                        duration_ms=duration_ms,
                    )

                await self.publisher.publish_test_result(outcome)

            except Exception as e:
                failed += 1
                duration_ms = int((time.monotonic() - test_start) * 1000)
                outcome = TestOutcome(
                    test_name=test.name,
                    category=TestCategory.SMOKE,
                    result=TestResult.ERROR,
                    duration_ms=duration_ms,
                    error=str(e),
                )
                await self.publisher.publish_test_result(outcome)
                logger.error("Smoke test error", test=test.name, error=str(e))

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.SMOKE,
            total=total,
            passed=passed,
            failed=failed,
            duration_ms=total_duration,
        )
        await self.publisher.publish_test_run_summary(summary)
        return summary

    async def run_golden_tests(self) -> TestRunSummary:
        """Run all golden path tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.golden)

        for test in self.config.tests.golden:
            test_start = time.monotonic()
            try:
                result, failed_step, error = await self._run_golden_test(
                    test.name, test.steps
                )
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if result:
                    passed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.GOLDEN,
                        result=TestResult.PASSED,
                        duration_ms=duration_ms,
                        critical=test.critical,
                    )
                    await self.publisher.publish_test_result(outcome)
                else:
                    failed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.GOLDEN,
                        result=TestResult.FAILED,
                        duration_ms=duration_ms,
                        critical=test.critical,
                        error=error,
                    )
                    await self.publisher.publish_test_result(outcome)

                    if test.critical:
                        failure = GoldenPathFailure(
                            test_name=test.name,
                            failed_step=failed_step or "unknown",
                            error=error or "Unknown error",
                            duration_ms=duration_ms,
                        )
                        await self.publisher.publish_golden_path_failure(failure)

            except Exception as e:
                failed += 1
                duration_ms = int((time.monotonic() - test_start) * 1000)
                logger.error("Golden test error", test=test.name, error=str(e))

                if test.critical:
                    failure = GoldenPathFailure(
                        test_name=test.name,
                        failed_step="execution",
                        error=str(e),
                        duration_ms=duration_ms,
                    )
                    await self.publisher.publish_golden_path_failure(failure)

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.GOLDEN,
            total=total,
            passed=passed,
            failed=failed,
            duration_ms=total_duration,
        )
        await self.publisher.publish_test_run_summary(summary)
        return summary

    async def _run_smoke_test(self, name: str, target: str) -> bool:
        """Run a single smoke test. Override in subclass with actual implementations."""
        logger.info("Running smoke test", name=name, target=target)
        # This will be implemented by importing actual test functions
        # For now, return True as placeholder
        return True

    async def _run_golden_test(
        self, name: str, steps: list[str]
    ) -> tuple[bool, str | None, str | None]:
        """Run a golden path test. Returns (success, failed_step, error)."""
        logger.info("Running golden test", name=name, steps=steps)
        # This will be implemented by importing actual test functions
        # For now, return success as placeholder
        return True, None, None

    async def start(self) -> None:
        """Start the test runner with scheduled execution."""
        self._running = True
        logger.info("Starting test runner")

        smoke_interval = self.config.schedules.smoke_seconds()
        golden_interval = self.config.schedules.golden_seconds()

        # Run initial tests
        await self.run_smoke_tests()
        await self.run_golden_tests()

        # Schedule recurring runs
        smoke_task = asyncio.create_task(self._schedule_smoke(smoke_interval))
        golden_task = asyncio.create_task(self._schedule_golden(golden_interval))

        try:
            await asyncio.gather(smoke_task, golden_task)
        except asyncio.CancelledError:
            logger.info("Test runner stopped")

    async def _schedule_smoke(self, interval: int) -> None:
        """Schedule smoke tests at interval."""
        while self._running:
            await asyncio.sleep(interval)
            if self._running:
                await self.run_smoke_tests()

    async def _schedule_golden(self, interval: int) -> None:
        """Schedule golden tests at interval."""
        while self._running:
            await asyncio.sleep(interval)
            if self._running:
                await self.run_golden_tests()

    def stop(self) -> None:
        """Stop the test runner."""
        self._running = False


async def main() -> None:
    """Main entry point."""
    import logging

    structlog.configure(
        wrapper_class=structlog.make_filtering_bound_logger(logging.INFO),
    )

    settings = Settings()
    logger.info("Loading config", path=str(settings.config_path))

    config = load_config(settings.config_path)
    logger.info(
        "Config loaded",
        services=len(config.services),
        smoke_tests=len(config.tests.smoke),
        golden_tests=len(config.tests.golden),
    )

    runner = TestRunner(settings, config)
    await runner.start()


if __name__ == "__main__":
    asyncio.run(main())
