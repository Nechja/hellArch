"""Pydantic models for the testing service."""

from datetime import UTC, datetime
from enum import Enum
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class TestCategory(str, Enum):
    """Test category types."""

    SMOKE = "smoke"
    INTEGRATION = "integration"
    GOLDEN = "golden"


class TestResult(str, Enum):
    """Test result status."""

    PASSED = "passed"
    FAILED = "failed"
    SKIPPED = "skipped"
    ERROR = "error"


class TestOutcome(BaseModel):
    """Result of a single test execution."""

    test_name: str
    category: TestCategory
    result: TestResult
    duration_ms: int
    critical: bool = False
    error: str | None = None
    timestamp: datetime = Field(default_factory=lambda: datetime.now(UTC))


class TestRunSummary(BaseModel):
    """Summary of a test run."""

    category: TestCategory
    total: int
    passed: int
    failed: int
    skipped: int = 0
    duration_ms: int
    timestamp: datetime = Field(default_factory=lambda: datetime.now(UTC))


class GoldenPathFailure(BaseModel):
    """Details of a golden path test failure."""

    test_name: str
    failed_step: str
    error: str
    duration_ms: int
    timestamp: datetime = Field(default_factory=lambda: datetime.now(UTC))


class CloudEvent(BaseModel):
    """CloudEvents specification model."""

    specversion: str = "1.0"
    id: UUID = Field(default_factory=uuid4)
    source: str = "hellarch/testing"
    type: str
    datacontenttype: str = "application/json"
    time: datetime = Field(default_factory=lambda: datetime.now(UTC))
    data: dict


class TokenResponse(BaseModel):
    """Auth token response."""

    access_token: str
    token_type: str = "Bearer"
    expires_in: int | None = None


class FileMetadata(BaseModel):
    """File metadata from files service."""

    id: str
    filename: str
    size: int
    content_type: str | None = None
    created_at: datetime | None = None


class EventRecord(BaseModel):
    """Event record from events service."""

    id: str
    type: str
    source: str
    time: datetime
    data: dict


class ServiceHealth(BaseModel):
    """Service health status."""

    service: str
    healthy: bool
    latency_ms: int | None = None
    error: str | None = None
