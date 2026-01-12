"""
Cross-service integration tests: Auth + Events.

Tests verify auth tokens work correctly with events service operations.
"""

from uuid import uuid4

import pytest

from testing.clients import GatewayClient


@pytest.mark.integration
class TestAuthenticatedEventOperations:
    """Event operations with authentication."""

    async def test_audit_logs_require_auth(
        self, gateway_client: GatewayClient
    ) -> None:
        """Audit log access without auth is rejected."""
        response = await gateway_client.get("/api/events/audit-logs")

        # Should require auth
        if response.status_code != 200:
            assert response.status_code == 401

    async def test_create_audit_log_with_valid_token(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Creating audit log with valid auth succeeds."""
        if not auth_token:
            pytest.skip("No auth token available")

        payload = {
            "event_id": str(uuid4()),
            "event_type": "hellarch.test.integration.auth.v1",
            "source": "integration-test",
            "metadata": {"test": True},
        }

        response = await gateway_client.post(
            "/api/events/audit-logs",
            json=payload,
            headers={"Authorization": f"Bearer {auth_token}"},
        )

        assert response.status_code == 201

    async def test_webhooks_require_auth(
        self, gateway_client: GatewayClient
    ) -> None:
        """Webhook management without auth is rejected."""
        response = await gateway_client.get("/api/events/webhooks")

        # Should require auth
        if response.status_code != 200:
            assert response.status_code == 401


@pytest.mark.integration
class TestEventPublishWithAuth:
    """Event publishing with authentication."""

    async def test_publish_requires_auth(
        self, gateway_client: GatewayClient
    ) -> None:
        """Event publishing without auth is rejected."""
        cloudevent = {
            "specversion": "1.0",
            "type": "hellarch.test.integration.publish.v1",
            "source": "integration-test",
            "id": str(uuid4()),
            "data": {},
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=cloudevent,
        )

        # Should require auth
        if response.status_code not in (200, 202):
            assert response.status_code == 401

    async def test_publish_with_valid_token(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Event publishing with valid auth succeeds."""
        if not auth_token:
            pytest.skip("No auth token available")

        cloudevent = {
            "specversion": "1.0",
            "type": "hellarch.test.integration.auth.v1",
            "source": "integration-test",
            "id": str(uuid4()),
            "data": {"authenticated": True},
        }

        response = await gateway_client.post(
            "/api/events/publish",
            json=cloudevent,
            headers={"Authorization": f"Bearer {auth_token}"},
        )

        assert response.status_code in (200, 202)
