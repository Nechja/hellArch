"""Golden path test: Authentication flow."""

import pytest

from testing.clients import AuthClient, GatewayClient


@pytest.mark.golden
class TestAuthFlow:
    """Complete authentication flow through gateway."""

    async def test_obtain_token(
        self,
        auth_client: AuthClient,
        test_credentials: tuple[str, str],
    ) -> None:
        """Step 1: Can obtain a token from auth service."""
        client_id, client_secret = test_credentials
        if not client_id:
            pytest.skip("Test credentials not configured")

        token_response = await auth_client.get_token(client_id, client_secret)

        assert token_response is not None, "Failed to obtain token"
        assert token_response.access_token, "Token is empty"

    async def test_validate_token(
        self,
        auth_client: AuthClient,
        auth_token: str,
    ) -> None:
        """Step 2: Token validates at auth service."""
        is_valid = await auth_client.validate_token(auth_token)
        assert is_valid, "Token validation failed"

    async def test_gateway_accepts_token(
        self,
        gateway_client: GatewayClient,
        auth_token: str,
    ) -> None:
        """Step 3: Gateway accepts the token."""
        is_valid = await gateway_client.check_auth(auth_token)
        assert is_valid, "Gateway rejected valid token"


@pytest.mark.golden
async def test_full_auth_flow(
    auth_client: AuthClient,
    gateway_client: GatewayClient,
    test_credentials: tuple[str, str],
) -> None:
    """Complete auth flow as a single test."""
    client_id, client_secret = test_credentials
    if not client_id:
        pytest.skip("Test credentials not configured")

    # Step 1: Obtain token
    token_response = await auth_client.get_token(client_id, client_secret)
    assert token_response is not None, "Failed to obtain token"

    token = token_response.access_token

    # Step 2: Validate at auth
    is_valid = await auth_client.validate_token(token)
    assert is_valid, "Token validation failed at auth service"

    # Step 3: Use at gateway
    gateway_valid = await gateway_client.check_auth(token)
    assert gateway_valid, "Gateway rejected valid token"
