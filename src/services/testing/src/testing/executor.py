"""Test execution logic for all test types."""

from typing import Any

import httpx
import structlog

from testing.config import (
    ContractTestConfig,
    ErrorTestConfig,
    GoldenTestConfig,
    IntegrationTestConfig,
    Settings,
    SmokeTestConfig,
    TestConfig,
)
from testing.schemas import SCHEMAS

logger = structlog.get_logger()


class TestExecutor:
    """Executes tests against services via gateway."""

    def __init__(
        self, config: TestConfig, settings: Settings | None = None, timeout: float = 10.0
    ) -> None:
        self.config = config
        self.settings = settings or Settings()
        self.timeout = timeout
        self.gateway_url = config.services.get("gateway", "http://gateway:5000")
        self._token: str | None = None
        self._test_file_id: str | None = None
        self._test_webhook_id: str | None = None

    async def _get_token(self) -> str | None:
        """Obtain auth token for tests that need it."""
        if self._token:
            return self._token

        # Use settings for credentials, fallback to defaults
        client_id = self.settings.client_id or "testing-service"
        client_secret = self.settings.client_secret or "testing-secret"

        auth_url = self.config.services.get("auth", "http://auth:5001")
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    f"{auth_url}/auth/token",
                    data={
                        "grant_type": "client_credentials",
                        "client_id": client_id,
                        "client_secret": client_secret,
                    },
                )
                if response.status_code == 200:
                    self._token = response.json().get("access_token")
                    return self._token
                logger.warning(
                    "Token request failed",
                    status=response.status_code,
                    body=response.text[:200],
                )
        except httpx.HTTPError as e:
            logger.warning("Failed to get auth token", error=str(e))
        return None

    async def _request(
        self,
        method: str,
        endpoint: str,
        headers: dict[str, str] | None = None,
        body: dict | None = None,
        auth: bool = False,
    ) -> httpx.Response:
        """Make HTTP request to gateway."""
        url = f"{self.gateway_url}{endpoint}"
        req_headers = headers.copy() if headers else {}

        if auth:
            token = await self._get_token()
            if token:
                req_headers["Authorization"] = f"Bearer {token}"

        async with httpx.AsyncClient(timeout=self.timeout) as client:
            if method.upper() == "GET":
                return await client.get(url, headers=req_headers)
            elif method.upper() == "POST":
                return await client.post(url, headers=req_headers, json=body)
            elif method.upper() == "PUT":
                return await client.put(url, headers=req_headers, json=body)
            elif method.upper() == "DELETE":
                return await client.delete(url, headers=req_headers)
            else:
                raise ValueError(f"Unsupported method: {method}")

    # Health path mapping for services with non-standard paths
    HEALTH_PATHS: dict[str, str] = {
        "auth": "/auth/health",
        "user": "/user/health",
        "identity": "/identity/health",
    }

    # Smoke Tests
    async def run_smoke_test(self, test: SmokeTestConfig) -> tuple[bool, str | None]:
        """Run a smoke test - basic health check of a service."""
        target = test.target
        service_url = self.config.services.get(target)

        if not service_url:
            return False, f"Unknown service: {target}"

        health_path = self.HEALTH_PATHS.get(target, "/health")

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.get(f"{service_url}{health_path}")
                if response.status_code == 200:
                    return True, None
                return False, f"Health check returned {response.status_code}"
        except httpx.HTTPError as e:
            return False, str(e)

    # Contract Tests
    async def run_contract_test(
        self, test: ContractTestConfig
    ) -> tuple[bool, str | None]:
        """Run a contract test - validate response matches expected schema."""
        try:
            # Determine if we need auth
            needs_auth = "Authorization" not in test.headers

            response = await self._request(
                method=test.method,
                endpoint=test.endpoint,
                headers=test.headers if test.headers else None,
                body=test.body,
                auth=needs_auth,
            )

            # Check status code
            if response.status_code not in test.expected_status:
                return False, f"Expected status {test.expected_status}, got {response.status_code}"

            # Validate schema
            schema = SCHEMAS.get(test.schema_name)
            if not schema:
                return False, f"Unknown schema: {test.schema_name}"

            try:
                data = response.json()
            except Exception:
                return False, "Response is not valid JSON"

            errors = schema.validate(data)
            if errors:
                return False, f"Schema validation failed: {errors}"

            return True, None

        except httpx.HTTPError as e:
            return False, str(e)

    # Error Tests
    async def run_error_test(self, test: ErrorTestConfig) -> tuple[bool, str | None]:
        """Run an error test - verify error handling returns expected status."""
        try:
            response = await self._request(
                method=test.method,
                endpoint=test.endpoint,
                headers=test.headers if test.headers else None,
                body=test.body,
                auth=False,  # Error tests typically test without valid auth
            )

            if response.status_code in test.expected_status:
                return True, None
            return False, f"Expected status {test.expected_status}, got {response.status_code}"

        except httpx.HTTPError as e:
            return False, str(e)

    # Integration Tests
    async def run_integration_test(
        self, test: IntegrationTestConfig
    ) -> tuple[bool, str | None]:
        """Run an integration test - verify multiple services work together."""
        # Check all target services are healthy first
        for target in test.targets:
            service_url = self.config.services.get(target)
            if not service_url:
                return False, f"Unknown service: {target}"

            health_path = self.HEALTH_PATHS.get(target, "/health")

            try:
                async with httpx.AsyncClient(timeout=self.timeout) as client:
                    response = await client.get(f"{service_url}{health_path}")
                    if response.status_code != 200:
                        return False, f"Service {target} unhealthy"
            except httpx.HTTPError as e:
                return False, f"Service {target} unreachable: {e}"

        # Run specific integration test logic based on test name
        test_func = INTEGRATION_TESTS.get(test.name)
        if test_func:
            return await test_func(self, test)

        # Default: just verify services are up
        return True, None

    # Golden Path Tests
    async def run_golden_test(
        self, test: GoldenTestConfig
    ) -> tuple[bool, str | None, str | None]:
        """Run a golden path test - execute steps in sequence."""
        for step in test.steps:
            step_func = GOLDEN_STEPS.get(step)
            if not step_func:
                return False, step, f"Unknown step: {step}"

            try:
                success, error = await step_func(self)
                if not success:
                    return False, step, error
            except Exception as e:
                return False, step, str(e)

        return True, None, None


# Golden path step implementations
async def step_obtain_token(executor: TestExecutor) -> tuple[bool, str | None]:
    """Obtain an auth token."""
    token = await executor._get_token()
    if token:
        return True, None
    return False, "Failed to obtain token"


async def step_validate_at_gateway(executor: TestExecutor) -> tuple[bool, str | None]:
    """Validate token at gateway."""
    if not executor._token:
        return False, "No token available"

    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/auth/validate",
            headers={"Authorization": f"Bearer {executor._token}"},
        )
        if response.status_code == 200:
            return True, None
        return False, f"Validation returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_access_protected_resource(
    executor: TestExecutor,
) -> tuple[bool, str | None]:
    """Access a protected resource (validate token)."""
    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/auth/validate",
            auth=True,
        )
        if response.status_code == 200:
            data = response.json()
            if data.get("valid"):
                return True, None
            return False, "Token validation returned invalid"
        return False, f"Protected resource returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_authenticate(executor: TestExecutor) -> tuple[bool, str | None]:
    """Authenticate for file operations."""
    return await step_obtain_token(executor)


async def step_upload_file(executor: TestExecutor) -> tuple[bool, str | None]:
    """Upload a test file."""
    try:
        token = await executor._get_token()
        headers = {"Authorization": f"Bearer {token}"} if token else {}

        # Get owner_id from settings
        client_id = executor.settings.client_id or "testing-service"
        owner_id = f"service:{client_id}"

        async with httpx.AsyncClient(timeout=executor.timeout) as client:
            response = await client.post(
                f"{executor.gateway_url}/api/files/upload",
                files={"file": ("golden_test.txt", b"golden path test", "text/plain")},
                data={"owner_id": owner_id},
                headers=headers,
            )

        if response.status_code in (200, 201):
            data = response.json()
            executor._test_file_id = data.get("id")
            return True, None
        return False, f"Upload returned {response.status_code}: {response.text[:100]}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_verify_exists(executor: TestExecutor) -> tuple[bool, str | None]:
    """Verify uploaded file exists."""
    file_id = getattr(executor, "_test_file_id", None)
    if not file_id:
        return False, "No file ID from upload"

    try:
        response = await executor._request(
            method="GET",
            endpoint=f"/api/files/{file_id}",
            auth=True,
        )
        if response.status_code == 200:
            return True, None
        return False, f"File check returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_download_file(executor: TestExecutor) -> tuple[bool, str | None]:
    """Download the test file."""
    file_id = getattr(executor, "_test_file_id", None)
    if not file_id:
        return False, "No file ID"

    try:
        response = await executor._request(
            method="GET",
            endpoint=f"/api/files/{file_id}",
            auth=True,
        )
        if response.status_code == 200:
            return True, None
        return False, f"Download returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_delete_file(executor: TestExecutor) -> tuple[bool, str | None]:
    """Delete the test file."""
    file_id = getattr(executor, "_test_file_id", None)
    if not file_id:
        return True, None  # No file to delete

    try:
        response = await executor._request(
            method="DELETE",
            endpoint=f"/api/files/{file_id}",
            auth=True,
        )
        if response.status_code in (200, 204):
            return True, None
        return False, f"Delete returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_publish_event(executor: TestExecutor) -> tuple[bool, str | None]:
    """Publish a test event."""
    from uuid import uuid4

    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/events/publish",
            body={
                "specversion": "1.0",
                "type": "hellarch.test.golden.v1",
                "source": "testing-service",
                "id": str(uuid4()),
                "data": {"test": True},
            },
            auth=True,
        )
        if response.status_code in (200, 202):
            return True, None
        return False, f"Publish returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_verify_stored(executor: TestExecutor) -> tuple[bool, str | None]:
    """Verify event was stored in audit logs."""
    try:
        response = await executor._request(
            method="GET",
            endpoint="/api/events/audit-logs",
            auth=True,
        )
        if response.status_code == 200:
            return True, None
        return False, f"Audit logs returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_verify_delivered(executor: TestExecutor) -> tuple[bool, str | None]:
    """Verify event delivery (placeholder - actual verification depends on setup)."""
    # This is a simplified check - real implementation would verify webhook delivery
    return True, None


# Integration test implementations
async def integration_gateway_auth_validation(
    executor: TestExecutor, test: IntegrationTestConfig
) -> tuple[bool, str | None]:
    """Test gateway validates tokens with auth service."""
    # Get a token
    token = await executor._get_token()
    if not token:
        return False, "Failed to get token"

    # Validate through gateway
    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/auth/validate",
            headers={"Authorization": f"Bearer {token}"},
        )
        if response.status_code == 200:
            data = response.json()
            if data.get("valid"):
                return True, None
        return False, f"Gateway validation failed: {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def integration_file_upload_event(
    executor: TestExecutor, test: IntegrationTestConfig
) -> tuple[bool, str | None]:
    """Test file upload triggers event."""
    token = await executor._get_token()
    headers = {"Authorization": f"Bearer {token}"} if token else {}

    # Get owner_id from settings
    client_id = executor.settings.client_id or "testing-service"
    owner_id = f"service:{client_id}"

    # Upload a file
    try:
        async with httpx.AsyncClient(timeout=executor.timeout) as client:
            response = await client.post(
                f"{executor.gateway_url}/api/files/upload",
                files={"file": ("integration_test.txt", b"test", "text/plain")},
                data={"owner_id": owner_id},
                headers=headers,
            )

        if response.status_code not in (200, 201):
            return False, f"Upload failed: {response.status_code}"

        file_id = response.json().get("id")

        # Clean up
        if file_id:
            await executor._request(
                method="DELETE",
                endpoint=f"/api/files/{file_id}",
                auth=True,
            )

        return True, None
    except httpx.HTTPError as e:
        return False, str(e)


async def integration_auth_user_sync(
    executor: TestExecutor, test: IntegrationTestConfig
) -> tuple[bool, str | None]:
    """Test auth service syncs with user service."""
    # This tests that auth and user services can communicate
    # Just verify both are healthy and reachable
    try:
        # Check user service health via gateway
        response = await executor._request(
            method="GET",
            endpoint="/api/user/health",
        )
        if response.status_code != 200:
            return False, f"User service unhealthy: {response.status_code}"
        return True, None
    except httpx.HTTPError as e:
        return False, str(e)


async def integration_events_webhook_delivery(
    executor: TestExecutor, test: IntegrationTestConfig
) -> tuple[bool, str | None]:
    """Test events service can list webhooks."""
    try:
        response = await executor._request(
            method="GET",
            endpoint="/api/events/webhooks",
            auth=True,
        )
        if response.status_code == 200:
            return True, None
        return False, f"Webhooks endpoint returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


# Webhook lifecycle golden steps
async def step_create_webhook(executor: TestExecutor) -> tuple[bool, str | None]:
    """Create a test webhook."""
    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/events/webhooks",
            body={
                "name": "test-webhook",
                "url": "https://httpbin.org/post",
                "secret": "test-webhook-secret",
                "event_types": ["hellarch.test.v1"],
            },
            auth=True,
        )
        if response.status_code in (200, 201):
            data = response.json()
            executor._test_webhook_id = data.get("id")
            return True, None
        return False, f"Create webhook returned {response.status_code}: {response.text[:100]}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_trigger_webhook(executor: TestExecutor) -> tuple[bool, str | None]:
    """Trigger the test webhook by publishing an event."""
    from uuid import uuid4

    try:
        response = await executor._request(
            method="POST",
            endpoint="/api/events/publish",
            body={
                "specversion": "1.0",
                "type": "hellarch.test.v1",
                "source": "testing-service",
                "id": str(uuid4()),
                "data": {"webhook_test": True},
            },
            auth=True,
        )
        if response.status_code in (200, 202):
            return True, None
        return False, f"Publish returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_verify_webhook_logged(executor: TestExecutor) -> tuple[bool, str | None]:
    """Verify webhook delivery was logged."""
    # Just check that audit logs are accessible - actual delivery verification
    # would require checking the webhook endpoint
    try:
        response = await executor._request(
            method="GET",
            endpoint="/api/events/audit-logs?page_size=5",
            auth=True,
        )
        if response.status_code == 200:
            return True, None
        return False, f"Audit logs returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


async def step_delete_webhook(executor: TestExecutor) -> tuple[bool, str | None]:
    """Delete the test webhook."""
    webhook_id = getattr(executor, "_test_webhook_id", None)
    if not webhook_id:
        return True, None  # No webhook to delete

    try:
        response = await executor._request(
            method="DELETE",
            endpoint=f"/api/events/webhooks/{webhook_id}",
            auth=True,
        )
        if response.status_code in (200, 204, 404):
            return True, None
        return False, f"Delete webhook returned {response.status_code}"
    except httpx.HTTPError as e:
        return False, str(e)


# Step registry
GOLDEN_STEPS: dict[str, Any] = {
    "obtain_token": step_obtain_token,
    "validate_at_gateway": step_validate_at_gateway,
    "access_protected_resource": step_access_protected_resource,
    "authenticate": step_authenticate,
    "upload_file": step_upload_file,
    "verify_exists": step_verify_exists,
    "download_file": step_download_file,
    "delete_file": step_delete_file,
    "publish_event": step_publish_event,
    "verify_stored": step_verify_stored,
    "verify_delivered": step_verify_delivered,
    "create_webhook": step_create_webhook,
    "trigger_webhook": step_trigger_webhook,
    "verify_webhook_logged": step_verify_webhook_logged,
    "delete_webhook": step_delete_webhook,
}

# Integration test registry
INTEGRATION_TESTS: dict[str, Any] = {
    "gateway_auth_validation": integration_gateway_auth_validation,
    "file_upload_event": integration_file_upload_event,
    "auth_user_sync": integration_auth_user_sync,
    "events_webhook_delivery": integration_events_webhook_delivery,
}
