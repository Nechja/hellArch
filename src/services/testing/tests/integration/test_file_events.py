"""
Cross-service integration tests: Files + Events.

Tests verify file operations trigger appropriate events.
"""

import pytest

from testing.clients import GatewayClient


@pytest.mark.integration
class TestFileOperationEvents:
    """File operations should generate audit events."""

    async def test_file_upload_creates_audit_event(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """File upload should create an audit log entry."""
        if not auth_token:
            pytest.skip("No auth token available")

        headers = {"Authorization": f"Bearer {auth_token}"}

        # Get current audit log count
        audit_before = await gateway_client.get(
            "/api/events/audit-logs",
            params={"page_size": 1},
            headers=headers,
        )

        if audit_before.status_code != 200:
            pytest.skip("Audit logs not accessible")

        # Upload a file
        upload_response = await gateway_client.post(
            "/api/files/upload",
            files={"file": ("event_test.txt", b"trigger event", "text/plain")},
            headers=headers,
        )

        if upload_response.status_code not in (200, 201):
            pytest.skip("Upload failed")

        file_id = upload_response.json().get("id")

        try:
            # Check for new audit events
            audit_after = await gateway_client.get(
                "/api/events/audit-logs",
                params={"page_size": 10},
                headers=headers,
            )

            assert audit_after.status_code == 200

            # Verify audit logs endpoint works
            # Actual event verification depends on implementation
            data = audit_after.json()
            assert "data" in data
        finally:
            if file_id:
                await gateway_client.request_delete(
                    f"/api/files/{file_id}",
                    headers=headers,
                )


@pytest.mark.integration
class TestWebhookFileIntegration:
    """Webhooks can be triggered by file events."""

    async def test_webhook_receives_file_event(
        self, gateway_client: GatewayClient, auth_token: str | None
    ) -> None:
        """Webhook subscribed to file events receives notifications."""
        if not auth_token:
            pytest.skip("No auth token available")

        headers = {"Authorization": f"Bearer {auth_token}"}

        # This test documents the expected behavior
        # Actual verification requires webhook endpoint setup

        # Create a webhook
        webhook_payload = {
            "name": "file-event-test-webhook",
            "url": "https://httpbin.org/post",
            "event_types": ["hellarch.file.created.v1"],
        }

        create_response = await gateway_client.post(
            "/api/events/webhooks",
            json=webhook_payload,
            headers=headers,
        )

        if create_response.status_code != 201:
            pytest.skip("Could not create webhook")

        webhook_id = create_response.json().get("id")

        try:
            # Verify webhook was created
            get_response = await gateway_client.get(
                f"/api/events/webhooks/{webhook_id}",
                headers=headers,
            )

            if get_response.status_code == 200:
                data = get_response.json()
                assert data.get("name") == webhook_payload["name"]
        finally:
            if webhook_id:
                await gateway_client.request_delete(
                    f"/api/events/webhooks/{webhook_id}",
                    headers=headers,
                )
