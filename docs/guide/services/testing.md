# Testing Service

**Python + FastAPI + Playwright**

Automated test orchestration service for continuous validation of HellArch services.

## Overview

| Property | Value |
|----------|-------|
| Language | Python 3.12 |
| Framework | FastAPI |
| Port | 5007 |
| Database | None (stateless) |
| Dependencies | Events, Files, All services under test |

## Responsibilities

- Execute scheduled test suites (smoke, contract, errors, integration, golden, e2e)
- Provide API for on-demand test triggering
- Publish test results as CloudEvents
- Capture and upload E2E screenshots

## API Endpoints

### Health & Status

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| GET | `/status` | Service status with test counts |

### Test Triggers

| Method | Path | Description |
|--------|------|-------------|
| POST | `/run/smoke` | Run smoke tests |
| POST | `/run/contract` | Run contract tests |
| POST | `/run/errors` | Run error path tests |
| POST | `/run/integration` | Run integration tests |
| POST | `/run/golden` | Run golden path tests |
| POST | `/run/e2e` | Run E2E browser tests |
| POST | `/run/all` | Run all test categories |

## Architecture

```
src/services/testing/
├── Dockerfile
├── pyproject.toml
└── src/testing/
    ├── __init__.py
    ├── api.py           # FastAPI routes
    ├── config.py        # Configuration models
    ├── models.py        # Test result models
    ├── runner.py        # Test orchestration
    ├── executor.py      # API test execution
    ├── e2e_executor.py  # Playwright E2E execution
    ├── events.py        # CloudEvent publishing
    ├── clients.py       # HTTP clients for services
    └── schemas.py       # JSON schema definitions
```

## Test Categories

### Smoke Tests
Health checks for all services. Runs every 30 seconds.

### Contract Tests
Validates API responses match JSON schemas. Ensures API contracts are maintained.

### Error Tests
Verifies proper error handling for edge cases like malformed tokens, missing fields, and invalid requests.

### Integration Tests
Tests cross-service communication patterns (e.g., gateway→auth validation).

### Golden Path Tests
Critical business flows that must always work. Failures are published as high-priority events.

### E2E Tests
Browser-based tests using Playwright. Captures screenshots and tests UI rendering.

## CloudEvents Published

| Event Type | Description |
|------------|-------------|
| `hellarch.test.passed.v1` | Test passed |
| `hellarch.test.failed.v1` | Test failed |
| `hellarch.test.error.v1` | Test execution error |
| `hellarch.test.run_completed.v1` | Test run summary |
| `hellarch.test.golden_path_failed.v1` | Critical golden path failure |
| `hellarch.test.e2e.passed.v1` | E2E test passed |
| `hellarch.test.e2e.failed.v1` | E2E test failed |
| `hellarch.test.e2e.critical_failed.v1` | Critical E2E failure |

## Configuration

Tests are configured via JSON file (`infra/config/tests.json`):

```json
{
  "services": {
    "gateway": "http://gateway:5000",
    "auth": "http://auth:5001"
  },
  "schedules": {
    "smoke": "30s",
    "contract": "5m",
    "e2e": "15m"
  },
  "tests": {
    "smoke": [...],
    "contract": [...],
    "e2e": [...]
  }
}
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `5007` | HTTP server port |
| `TESTING_CONFIG_PATH` | `/config/tests.json` | Test configuration file |
| `TESTING_EVENTS_URL` | `http://events:5005` | Events service URL |
| `TESTING_FILES_URL` | `http://files:5006` | Files service URL (screenshot uploads) |
| `TESTING_CLIENT_ID` | `testing-service` | Service account ID |
| `TESTING_CLIENT_SECRET` | - | Service account secret |
| `TESTING_LOG_LEVEL` | `INFO` | Log level |
| `OTEL_SERVICE_NAME` | `testing` | OpenTelemetry service name |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | - | OTLP endpoint for tracing |

## Docker

```yaml
testing:
  build:
    context: ../../src/services/testing
    dockerfile: Dockerfile
  image: hellarch-testing
  container_name: hellarch-testing
  ports:
    - "5007:5007"
  environment:
    - PORT=5007
    - TESTING_CONFIG_PATH=/config/tests.json
    - TESTING_EVENTS_URL=http://events:5005
  volumes:
    - ../../infra/config/tests.json:/config/tests.json:ro
```

## Dependencies

The testing service depends on:

| Service | Purpose |
|---------|---------|
| Events | Publishing test results |
| Files | Uploading E2E screenshots |
| Auth | Service token for authenticated tests |
| All services | Targets for testing |

## Scheduled Execution

Tests run on configurable schedules:

| Category | Default Interval |
|----------|-----------------|
| Smoke | 30 seconds |
| Contract | 5 minutes |
| Errors | 5 minutes |
| Integration | 2 minutes |
| Golden | 5 minutes |
| E2E | 15 minutes |

Schedules are defined in `tests.json` using duration strings (`30s`, `5m`, `1h`).

## Playwright Browser

E2E tests use Chromium via Playwright. The Docker image includes:
- Playwright Python package
- Chromium browser
- Required system libraries (libnss3, libatk, etc.)

Browser runs in headless mode with sandbox disabled for container compatibility.

## Related Documentation

- [Testing Guide](/guide/testing) - Full testing documentation
- [Events Service](/guide/services/events) - Event publishing
- [Files Service](/guide/services/files) - Screenshot storage
- [Admin Console](/guide/clients/admin) - Test UI
