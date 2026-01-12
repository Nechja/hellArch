"""
Error path tests for Events service via Gateway.

Tests verify proper error handling for invalid inputs and edge cases.
"""

from uuid import uuid4

import pytest

from testing.clients import GatewayClient


@pytest.mark.errors
class TestAuditLogErrors:
    """Error scenarios for /api/events/audit-logs."""

    async def test_create_audit_log_missing_event_type(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Missing event_type returns 422."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "event_id": str(uuid4()),
            "source": "error-test",
            "metadata": {},
        }

        response = await gateway_client.post(
            "/api/events/audit-logs",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Audit logs require authentication")

        assert response.status_code == 422

    async def test_create_audit_log_invalid_event_id(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Invalid event_id format returns 422."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "event_id": "not-a-uuid",
            "event_type": "hellarch.test.error.v1",
            "source": "error-test",
        }

        response = await gateway_client.post(
            "/api/events/audit-logs",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Audit logs require authentication")

        assert response.status_code in (400, 422)

    async def test_get_audit_log_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Non-existent audit log returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            f"/api/events/audit-logs/{uuid4()}",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Audit logs require authentication")

        assert response.status_code == 404


@pytest.mark.errors
class TestWebhookErrors:
    """Error scenarios for /api/events/webhooks."""

    async def test_create_webhook_missing_url(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Missing url returns 422."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "name": "error-test-webhook",
            "event_types": ["hellarch.test.v1"],
        }

        response = await gateway_client.post(
            "/api/events/webhooks",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Webhooks require authentication")

        assert response.status_code == 422

    async def test_create_webhook_invalid_url(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Invalid url format returns 422."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "name": "error-test-webhook",
            "url": "not-a-valid-url",
            "event_types": ["hellarch.test.v1"],
        }

        response = await gateway_client.post(
            "/api/events/webhooks",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Webhooks require authentication")

        assert response.status_code in (400, 422)

    async def test_get_webhook_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Non-existent webhook returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.get(
            f"/api/events/webhooks/{uuid4()}",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Webhooks require authentication")

        assert response.status_code == 404

    async def test_delete_webhook_not_found(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Deleting non-existent webhook returns 404."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        response = await gateway_client.request_delete(
            f"/api/events/webhooks/{uuid4()}",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Webhooks require authentication")

        assert response.status_code == 404


@pytest.mark.errors
class TestEventPublishErrors:
    """Error scenarios for /api/events/publish."""

    async def test_publish_missing_type(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """CloudEvent missing type returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "specversion": "1.0",
            "source": "error-test",
            "id": str(uuid4()),
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing requires authentication")

        assert response.status_code in (400, 422)

    async def test_publish_missing_source(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """CloudEvent missing source returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "specversion": "1.0",
            "type": "hellarch.test.error.v1",
            "id": str(uuid4()),
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing requires authentication")

        assert response.status_code in (400, 422)

    async def test_publish_invalid_specversion(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """CloudEvent with invalid specversion returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}

        payload = {
            "specversion": "2.0",  # Invalid - only 1.0 supported
            "type": "hellarch.test.error.v1",
            "source": "error-test",
            "id": str(uuid4()),
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=payload,
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing requires authentication")

        assert response.status_code in (400, 422)

    async def test_publish_malformed_json(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Malformed JSON returns 400."""
        headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}
        headers["Content-Type"] = "application/json"

        response = await gateway_client.post(
            "/api/events/publish",
            content=b"not valid json",
            headers=headers,
        )

        if response.status_code == 401:
            pytest.skip("Publishing requires authentication")

        assert response.status_code in (400, 422)
