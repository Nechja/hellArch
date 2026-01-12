"""Smoke tests: Service availability checks."""

import pytest

from testing.clients import AuthClient, EventsClient, FilesClient, GatewayClient


@pytest.mark.smoke
async def test_gateway_responds(gateway_client: GatewayClient) -> None:
    """Gateway service is responding."""
    response = await gateway_client.get("/health")
    assert response.status_code == 200


@pytest.mark.smoke
async def test_auth_responds(auth_client: AuthClient) -> None:
    """Auth service is responding."""
    response = await auth_client.get("/auth/health")
    assert response.status_code == 200


@pytest.mark.smoke
async def test_events_responds(events_client: EventsClient) -> None:
    """Events service is responding."""
    response = await events_client.get("/health")
    assert response.status_code == 200


@pytest.mark.smoke
async def test_files_responds(files_client: FilesClient) -> None:
    """Files service is responding."""
    response = await files_client.get("/health")
    assert response.status_code == 200


@pytest.mark.smoke
async def test_auth_can_issue_token(
    auth_client: AuthClient,
    test_credentials: tuple[str, str],
) -> None:
    """Auth service can issue tokens."""
    client_id, client_secret = test_credentials
    if not client_id:
        pytest.skip("Test credentials not configured")

    token_response = await auth_client.get_token(client_id, client_secret)
    assert token_response is not None
    assert token_response.access_token


@pytest.mark.smoke
async def test_events_accepts_publish(events_client: EventsClient) -> None:
    """Events service accepts publish requests."""
    from testing.models import CloudEvent

    event = CloudEvent(
        type="hellarch.test.smoke.v1",
        data={"smoke": True},
    )
    result = await events_client.publish(event.model_dump(mode="json"))
    assert result


@pytest.mark.smoke
async def test_files_can_list(
    files_client: FilesClient,
    auth_token: str | None,
) -> None:
    """Files service can list files."""
    files = await files_client.list_files(token=auth_token)
    # Just verify we get a list back (may be empty)
    assert isinstance(files, list)
