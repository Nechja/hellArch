"""Configuration loading and models for the testing service."""

import json
from pathlib import Path

from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Environment-based settings."""

    config_path: Path = Field(default=Path("/config/tests.json"))
    events_url: str = Field(default="http://events:5005")
    files_url: str = Field(default="http://files:5006")
    log_level: str = Field(default="INFO")
    client_id: str = Field(default="")
    client_secret: str = Field(default="")

    model_config = {"env_prefix": "TESTING_"}


class SmokeTestConfig(BaseModel):
    """Smoke test configuration."""

    name: str
    description: str
    target: str


class IntegrationTestConfig(BaseModel):
    """Integration test configuration."""

    name: str
    description: str
    targets: list[str]


class GoldenTestConfig(BaseModel):
    """Golden path test configuration."""

    name: str
    description: str
    critical: bool = False
    steps: list[str]


class ContractTestConfig(BaseModel):
    """Contract/schema validation test configuration."""

    name: str
    description: str
    endpoint: str
    method: str = "GET"
    headers: dict[str, str] = Field(default_factory=dict)
    body: dict | None = None
    expected_status: list[int] = Field(default_factory=lambda: [200])
    schema_name: str


class ErrorTestConfig(BaseModel):
    """Error path test configuration."""

    name: str
    description: str
    endpoint: str
    method: str = "GET"
    headers: dict[str, str] = Field(default_factory=dict)
    body: dict | None = None
    expected_status: list[int]


class E2eStepConfig(BaseModel):
    """Single E2E test step configuration."""

    action: str  # navigate, click, fill, wait, assert_visible, assert_text, screenshot
    selector: str | None = None
    value: str | None = None
    screenshot_name: str | None = None


class E2eTestConfig(BaseModel):
    """E2E browser test configuration."""

    name: str
    description: str
    base_url: str
    steps: list[E2eStepConfig]
    screenshot_on_failure: bool = True
    critical: bool = False


class ScheduleConfig(BaseModel):
    """Test schedule intervals."""

    smoke: str = "30s"
    integration: str = "2m"
    golden: str = "5m"
    contract: str = "10m"
    errors: str = "10m"
    e2e: str = "15m"

    def smoke_seconds(self) -> int:
        return parse_interval(self.smoke)

    def integration_seconds(self) -> int:
        return parse_interval(self.integration)

    def golden_seconds(self) -> int:
        return parse_interval(self.golden)

    def contract_seconds(self) -> int:
        return parse_interval(self.contract)

    def errors_seconds(self) -> int:
        return parse_interval(self.errors)

    def e2e_seconds(self) -> int:
        return parse_interval(self.e2e)


class TestsConfig(BaseModel):
    """Tests section of configuration."""

    smoke: list[SmokeTestConfig] = Field(default_factory=list)
    integration: list[IntegrationTestConfig] = Field(default_factory=list)
    golden: list[GoldenTestConfig] = Field(default_factory=list)
    contract: list[ContractTestConfig] = Field(default_factory=list)
    errors: list[ErrorTestConfig] = Field(default_factory=list)
    e2e: list[E2eTestConfig] = Field(default_factory=list)


class TestConfig(BaseModel):
    """Full test configuration."""

    services: dict[str, str]
    schedules: ScheduleConfig = Field(default_factory=ScheduleConfig)
    tests: TestsConfig = Field(default_factory=TestsConfig)

    def get_service_url(self, service: str) -> str | None:
        """Get URL for a service."""
        return self.services.get(service)


def load_config(path: Path) -> TestConfig:
    """Load test configuration from JSON file."""
    with path.open() as f:
        data = json.load(f)

    # Handle nested tests structure from JSON
    if "tests" in data and isinstance(data["tests"], dict):
        tests_data = data["tests"]
        data["tests"] = TestsConfig(
            smoke=[SmokeTestConfig(**t) for t in tests_data.get("smoke", [])],
            integration=[IntegrationTestConfig(**t) for t in tests_data.get("integration", [])],
            golden=[GoldenTestConfig(**t) for t in tests_data.get("golden", [])],
            contract=[ContractTestConfig(**t) for t in tests_data.get("contract", [])],
            errors=[ErrorTestConfig(**t) for t in tests_data.get("errors", [])],
            e2e=[E2eTestConfig(**t) for t in tests_data.get("e2e", [])],
        )

    return TestConfig.model_validate(data)


def parse_interval(interval: str) -> int:
    """Parse interval string to seconds (e.g., '30s', '2m', '5m')."""
    if interval.endswith("s"):
        return int(interval[:-1])
    if interval.endswith("m"):
        return int(interval[:-1]) * 60
    if interval.endswith("h"):
        return int(interval[:-1]) * 3600
    return int(interval)
