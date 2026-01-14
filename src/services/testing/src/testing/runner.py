"""Main test runner for the testing service."""

import asyncio
import time
from collections.abc import Callable, Coroutine
from typing import Any

import structlog

from testing.config import Settings, TestConfig, load_config
from testing.e2e_executor import E2eExecutor
from testing.events import TestEventPublisher
from testing.executor import TestExecutor
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
        self.executor = TestExecutor(config, settings)
        self.e2e_executor = E2eExecutor(settings)
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
                success, error = await self.executor.run_smoke_test(test)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if success:
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
                        error=error,
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

    async def run_contract_tests(self) -> TestRunSummary:
        """Run all contract tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.contract)

        for test in self.config.tests.contract:
            test_start = time.monotonic()
            try:
                success, error = await self.executor.run_contract_test(test)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if success:
                    passed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.CONTRACT,
                        result=TestResult.PASSED,
                        duration_ms=duration_ms,
                    )
                else:
                    failed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.CONTRACT,
                        result=TestResult.FAILED,
                        duration_ms=duration_ms,
                        error=error,
                    )

                await self.publisher.publish_test_result(outcome)

            except Exception as e:
                failed += 1
                duration_ms = int((time.monotonic() - test_start) * 1000)
                outcome = TestOutcome(
                    test_name=test.name,
                    category=TestCategory.CONTRACT,
                    result=TestResult.ERROR,
                    duration_ms=duration_ms,
                    error=str(e),
                )
                await self.publisher.publish_test_result(outcome)
                logger.error("Contract test error", test=test.name, error=str(e))

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.CONTRACT,
            total=total,
            passed=passed,
            failed=failed,
            duration_ms=total_duration,
        )
        await self.publisher.publish_test_run_summary(summary)
        return summary

    async def run_error_tests(self) -> TestRunSummary:
        """Run all error path tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.errors)

        for test in self.config.tests.errors:
            test_start = time.monotonic()
            try:
                success, error = await self.executor.run_error_test(test)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if success:
                    passed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.ERRORS,
                        result=TestResult.PASSED,
                        duration_ms=duration_ms,
                    )
                else:
                    failed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.ERRORS,
                        result=TestResult.FAILED,
                        duration_ms=duration_ms,
                        error=error,
                    )

                await self.publisher.publish_test_result(outcome)

            except Exception as e:
                failed += 1
                duration_ms = int((time.monotonic() - test_start) * 1000)
                outcome = TestOutcome(
                    test_name=test.name,
                    category=TestCategory.ERRORS,
                    result=TestResult.ERROR,
                    duration_ms=duration_ms,
                    error=str(e),
                )
                await self.publisher.publish_test_result(outcome)
                logger.error("Error test error", test=test.name, error=str(e))

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.ERRORS,
            total=total,
            passed=passed,
            failed=failed,
            duration_ms=total_duration,
        )
        await self.publisher.publish_test_run_summary(summary)
        return summary

    async def run_integration_tests(self) -> TestRunSummary:
        """Run all integration tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.integration)

        for test in self.config.tests.integration:
            test_start = time.monotonic()
            try:
                success, error = await self.executor.run_integration_test(test)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if success:
                    passed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.INTEGRATION,
                        result=TestResult.PASSED,
                        duration_ms=duration_ms,
                    )
                else:
                    failed += 1
                    outcome = TestOutcome(
                        test_name=test.name,
                        category=TestCategory.INTEGRATION,
                        result=TestResult.FAILED,
                        duration_ms=duration_ms,
                        error=error,
                    )

                await self.publisher.publish_test_result(outcome)

            except Exception as e:
                failed += 1
                duration_ms = int((time.monotonic() - test_start) * 1000)
                outcome = TestOutcome(
                    test_name=test.name,
                    category=TestCategory.INTEGRATION,
                    result=TestResult.ERROR,
                    duration_ms=duration_ms,
                    error=str(e),
                )
                await self.publisher.publish_test_result(outcome)
                logger.error("Integration test error", test=test.name, error=str(e))

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.INTEGRATION,
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
                success, failed_step, error = await self.executor.run_golden_test(test)
                duration_ms = int((time.monotonic() - test_start) * 1000)

                if success:
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

    async def run_e2e_tests(self) -> TestRunSummary:
        """Run all E2E browser tests."""
        start = time.monotonic()
        passed = 0
        failed = 0
        total = len(self.config.tests.e2e)

        for test in self.config.tests.e2e:
            outcome = await self.e2e_executor.run_e2e_test(test)

            if outcome.result == TestResult.PASSED:
                passed += 1
            else:
                failed += 1

            # Publish E2E result with screenshots
            await self.publisher.publish_e2e_result(outcome)

            if outcome.critical and outcome.result != TestResult.PASSED:
                await self.publisher.publish_e2e_failure(outcome)

        total_duration = int((time.monotonic() - start) * 1000)
        summary = TestRunSummary(
            category=TestCategory.E2E,
            total=total,
            passed=passed,
            failed=failed,
            duration_ms=total_duration,
        )
        await self.publisher.publish_test_run_summary(summary)
        return summary

    async def run_all_tests(self) -> list[TestRunSummary]:
        """Run all test categories."""
        summaries = []
        summaries.append(await self.run_smoke_tests())
        summaries.append(await self.run_contract_tests())
        summaries.append(await self.run_error_tests())
        summaries.append(await self.run_integration_tests())
        summaries.append(await self.run_golden_tests())
        summaries.append(await self.run_e2e_tests())
        return summaries

    async def start(self) -> None:
        """Start the test runner with scheduled execution."""
        self._running = True
        logger.info("Starting test runner")

        # Run initial tests
        await self.run_all_tests()

        # Schedule recurring runs
        tasks = [
            asyncio.create_task(
                self._schedule_tests(
                    "smoke",
                    self.config.schedules.smoke_seconds(),
                    self.run_smoke_tests,
                )
            ),
            asyncio.create_task(
                self._schedule_tests(
                    "contract",
                    self.config.schedules.contract_seconds(),
                    self.run_contract_tests,
                )
            ),
            asyncio.create_task(
                self._schedule_tests(
                    "errors",
                    self.config.schedules.errors_seconds(),
                    self.run_error_tests,
                )
            ),
            asyncio.create_task(
                self._schedule_tests(
                    "integration",
                    self.config.schedules.integration_seconds(),
                    self.run_integration_tests,
                )
            ),
            asyncio.create_task(
                self._schedule_tests(
                    "golden",
                    self.config.schedules.golden_seconds(),
                    self.run_golden_tests,
                )
            ),
            asyncio.create_task(
                self._schedule_tests(
                    "e2e",
                    self.config.schedules.e2e_seconds(),
                    self.run_e2e_tests,
                )
            ),
        ]

        try:
            await asyncio.gather(*tasks)
        except asyncio.CancelledError:
            logger.info("Test runner stopped")

    async def _schedule_tests(
        self,
        name: str,
        interval: int,
        run_func: Callable[[], Coroutine[Any, Any, TestRunSummary]],
    ) -> None:
        """Schedule tests at interval."""
        while self._running:
            await asyncio.sleep(interval)
            if self._running:
                logger.info("Running scheduled tests", category=name)
                await run_func()

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
        contract_tests=len(config.tests.contract),
        error_tests=len(config.tests.errors),
        integration_tests=len(config.tests.integration),
        golden_tests=len(config.tests.golden),
        e2e_tests=len(config.tests.e2e),
    )

    runner = TestRunner(settings, config)
    await runner.start()


if __name__ == "__main__":
    asyncio.run(main())
