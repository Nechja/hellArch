"""HTTP API for triggering and monitoring test runs."""

from contextlib import asynccontextmanager
from datetime import UTC, datetime
from enum import Enum

import structlog
from fastapi import BackgroundTasks, FastAPI, HTTPException
from pydantic import BaseModel

from testing.config import Settings, TestConfig, load_config
from testing.runner import TestRunner

logger = structlog.get_logger()

# Global state
_runner: TestRunner | None = None
_config: TestConfig | None = None
_settings: Settings | None = None
_running_tests: dict[str, datetime] = {}


class RunStatus(str, Enum):
    """Test run status."""

    IDLE = "idle"
    RUNNING = "running"
    COMPLETED = "completed"


class TriggerResponse(BaseModel):
    """Response for test trigger requests."""

    status: str
    category: str
    message: str


class StatusResponse(BaseModel):
    """Response for status requests."""

    service: str
    status: str
    running_tests: dict[str, str]
    config_loaded: bool
    smoke_tests: int
    contract_tests: int
    error_tests: int
    integration_tests: int
    golden_tests: int


class HealthResponse(BaseModel):
    """Health check response."""

    status: str
    timestamp: datetime


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan manager."""
    global _runner, _config, _settings

    _settings = Settings()
    logger.info("Loading config", path=str(_settings.config_path))

    try:
        _config = load_config(_settings.config_path)
        _runner = TestRunner(_settings, _config)
        logger.info(
            "Testing service started",
            smoke_tests=len(_config.tests.smoke),
            contract_tests=len(_config.tests.contract),
            error_tests=len(_config.tests.errors),
            integration_tests=len(_config.tests.integration),
            golden_tests=len(_config.tests.golden),
        )
    except Exception as e:
        logger.error("Failed to load config", error=str(e))
        _config = None
        _runner = None

    yield

    logger.info("Testing service shutting down")


app = FastAPI(
    title="HellArch Testing Service",
    description="Integration and golden path testing for HellArch",
    version="0.1.0",
    lifespan=lifespan,
)


@app.get("/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """Health check endpoint."""
    return HealthResponse(status="healthy", timestamp=datetime.now(UTC))


@app.get("/status", response_model=StatusResponse)
async def get_status() -> StatusResponse:
    """Get service status."""
    return StatusResponse(
        service="testing",
        status="ready" if _runner else "not_configured",
        running_tests={k: v.isoformat() for k, v in _running_tests.items()},
        config_loaded=_config is not None,
        smoke_tests=len(_config.tests.smoke) if _config else 0,
        contract_tests=len(_config.tests.contract) if _config else 0,
        error_tests=len(_config.tests.errors) if _config else 0,
        integration_tests=len(_config.tests.integration) if _config else 0,
        golden_tests=len(_config.tests.golden) if _config else 0,
    )


async def _run_tests_background(category: str) -> None:
    """Run tests in background and update status."""
    global _running_tests

    if not _runner:
        return

    _running_tests[category] = datetime.now(UTC)

    try:
        if category == "smoke":
            await _runner.run_smoke_tests()
        elif category == "contract":
            await _runner.run_contract_tests()
        elif category == "errors":
            await _runner.run_error_tests()
        elif category == "integration":
            await _runner.run_integration_tests()
        elif category == "golden":
            await _runner.run_golden_tests()
        elif category == "all":
            await _runner.run_all_tests()
    except Exception as e:
        logger.error("Test run failed", category=category, error=str(e))
    finally:
        _running_tests.pop(category, None)


@app.post("/run/smoke", response_model=TriggerResponse)
async def trigger_smoke_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger smoke tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if "smoke" in _running_tests:
        return TriggerResponse(
            status="already_running",
            category="smoke",
            message="Smoke tests are already running",
        )

    background_tasks.add_task(_run_tests_background, "smoke")

    return TriggerResponse(
        status="started",
        category="smoke",
        message="Smoke tests started",
    )


@app.post("/run/contract", response_model=TriggerResponse)
async def trigger_contract_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger contract tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if "contract" in _running_tests:
        return TriggerResponse(
            status="already_running",
            category="contract",
            message="Contract tests are already running",
        )

    background_tasks.add_task(_run_tests_background, "contract")

    return TriggerResponse(
        status="started",
        category="contract",
        message="Contract tests started",
    )


@app.post("/run/errors", response_model=TriggerResponse)
async def trigger_error_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger error path tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if "errors" in _running_tests:
        return TriggerResponse(
            status="already_running",
            category="errors",
            message="Error tests are already running",
        )

    background_tasks.add_task(_run_tests_background, "errors")

    return TriggerResponse(
        status="started",
        category="errors",
        message="Error tests started",
    )


@app.post("/run/integration", response_model=TriggerResponse)
async def trigger_integration_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger integration tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if "integration" in _running_tests:
        return TriggerResponse(
            status="already_running",
            category="integration",
            message="Integration tests are already running",
        )

    background_tasks.add_task(_run_tests_background, "integration")

    return TriggerResponse(
        status="started",
        category="integration",
        message="Integration tests started",
    )


@app.post("/run/golden", response_model=TriggerResponse)
async def trigger_golden_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger golden path tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if "golden" in _running_tests:
        return TriggerResponse(
            status="already_running",
            category="golden",
            message="Golden path tests are already running",
        )

    background_tasks.add_task(_run_tests_background, "golden")

    return TriggerResponse(
        status="started",
        category="golden",
        message="Golden path tests started",
    )


@app.post("/run/all", response_model=TriggerResponse)
async def trigger_all_tests(background_tasks: BackgroundTasks) -> TriggerResponse:
    """Trigger all tests."""
    if not _runner:
        raise HTTPException(status_code=503, detail="Testing service not configured")

    if _running_tests:
        return TriggerResponse(
            status="already_running",
            category="all",
            message=f"Tests already running: {list(_running_tests.keys())}",
        )

    background_tasks.add_task(_run_tests_background, "all")

    return TriggerResponse(
        status="started",
        category="all",
        message="All tests started",
    )


def create_app() -> FastAPI:
    """Create the FastAPI application."""
    return app
