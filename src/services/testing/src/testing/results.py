"""In-memory store for test results with error tracking."""

from collections import deque
from dataclasses import dataclass, field
from threading import Lock

from .models import TestCategory, TestOutcome, TestResult, TestRunSummary


@dataclass
class ResultsStore:
    """Thread-safe in-memory store for recent test results."""

    max_results: int = 100
    max_failures: int = 50
    _results: deque[TestOutcome] = field(default_factory=deque)
    _failures: deque[TestOutcome] = field(default_factory=deque)
    _summaries: dict[TestCategory, TestRunSummary] = field(default_factory=dict)
    _lock: Lock = field(default_factory=Lock)

    def add_result(self, outcome: TestOutcome) -> None:
        """Add a test result to the store."""
        with self._lock:
            self._results.append(outcome)
            while len(self._results) > self.max_results:
                self._results.popleft()

            if outcome.result in (TestResult.FAILED, TestResult.ERROR):
                self._failures.append(outcome)
                while len(self._failures) > self.max_failures:
                    self._failures.popleft()

    def add_summary(self, summary: TestRunSummary) -> None:
        """Store the latest summary for a category."""
        with self._lock:
            self._summaries[summary.category] = summary

    def get_recent_results(
        self, limit: int = 50, category: TestCategory | None = None
    ) -> list[TestOutcome]:
        """Get recent test results, optionally filtered by category."""
        with self._lock:
            results = list(self._results)

        if category:
            results = [r for r in results if r.category == category]

        return results[-limit:]

    def get_failures(
        self, limit: int = 20, category: TestCategory | None = None
    ) -> list[TestOutcome]:
        """Get recent failures, optionally filtered by category."""
        with self._lock:
            failures = list(self._failures)

        if category:
            failures = [f for f in failures if f.category == category]

        return failures[-limit:]

    def get_summaries(self) -> dict[TestCategory, TestRunSummary]:
        """Get the latest summary for each category."""
        with self._lock:
            return dict(self._summaries)

    def get_summary(self, category: TestCategory) -> TestRunSummary | None:
        """Get the latest summary for a specific category."""
        with self._lock:
            return self._summaries.get(category)

    def clear(self) -> None:
        """Clear all stored results."""
        with self._lock:
            self._results.clear()
            self._failures.clear()
            self._summaries.clear()


# Global singleton for the results store
_store: ResultsStore | None = None


def get_results_store() -> ResultsStore:
    """Get or create the global results store."""
    global _store
    if _store is None:
        _store = ResultsStore()
    return _store
