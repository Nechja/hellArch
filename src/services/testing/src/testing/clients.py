"""HTTP clients for interacting with hellArch services."""

from typing import Any, Self

import httpx
import structlog
from pydantic import ValidationError

from testing.models import EventRecord, FileMetadata, TokenResponse

logger = structlog.get_logger()


class ServiceClient:
    """Base client for service interactions."""

    def __init__(self, base_url: str, timeout: float = 10.0) -> None:
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self._client: httpx.AsyncClient | None = None

    async def __aenter__(self) -> Self:
        self._client = httpx.AsyncClient(base_url=self.base_url, timeout=self.timeout)
        return self

    async def __aexit__(self, *_: Any) -> None:
        if self._client:
            await self._client.aclose()

    @property
    def client(self) -> httpx.AsyncClient:
        if self._client is None:
            raise RuntimeError("Client not initialized. Use async context manager.")
        return self._client

    async def get(self, path: str, **kwargs: Any) -> httpx.Response:
        return await self.client.get(path, **kwargs)

    async def post(self, path: str, **kwargs: Any) -> httpx.Response:
        return await self.client.post(path, **kwargs)

    async def put(self, path: str, **kwargs: Any) -> httpx.Response:
        return await self.client.put(path, **kwargs)

    async def request_delete(self, path: str, **kwargs: Any) -> httpx.Response:
        return await self.client.delete(path, **kwargs)


class AuthClient(ServiceClient):
    """Client for auth service interactions."""

    async def get_token(self, client_id: str, client_secret: str) -> TokenResponse | None:
        """Obtain an access token using client credentials."""
        try:
            response = await self.post(
                "/auth/token",
                data={
                    "grant_type": "client_credentials",
                    "client_id": client_id,
                    "client_secret": client_secret,
                },
            )
            if response.status_code == 200:
                return TokenResponse.model_validate(response.json())
        except httpx.HTTPError as e:
            logger.error("Failed to get token", error=str(e))
        except ValidationError as e:
            logger.error("Invalid token response", error=str(e))
        return None

    async def validate_token(self, token: str) -> bool:
        """Validate a token."""
        try:
            response = await self.post(
                "/auth/validate",
                json={"token": token},
            )
            return response.status_code == 200
        except httpx.HTTPError as e:
            logger.error("Failed to validate token", error=str(e))
            return False


class EventsClient(ServiceClient):
    """Client for events service interactions."""

    async def publish(self, event: dict[str, Any]) -> bool:
        """Publish a CloudEvent."""
        try:
            response = await self.post("/publish", json=event)
            return response.status_code in (200, 201, 202)
        except httpx.HTTPError as e:
            logger.error("Failed to publish event", error=str(e))
            return False

    async def get_events(
        self, event_type: str | None = None, limit: int = 10
    ) -> list[EventRecord]:
        """Retrieve events."""
        params: dict[str, Any] = {"limit": limit}
        if event_type:
            params["type"] = event_type
        try:
            response = await self.get("/events", params=params)
            if response.status_code == 200:
                events_data = response.json().get("events", [])
                return [EventRecord.model_validate(e) for e in events_data]
        except httpx.HTTPError as e:
            logger.error("Failed to get events", error=str(e))
        except ValidationError as e:
            logger.error("Invalid events response", error=str(e))
        return []


class FilesClient(ServiceClient):
    """Client for files service interactions."""

    def _auth_headers(self, token: str | None) -> dict[str, str]:
        """Build auth headers if token provided."""
        return {"Authorization": f"Bearer {token}"} if token else {}

    async def list_files(self, token: str | None = None) -> list[FileMetadata]:
        """List files."""
        try:
            response = await self.get("/files", headers=self._auth_headers(token))
            if response.status_code == 200:
                files_data = response.json().get("files", [])
                return [FileMetadata.model_validate(f) for f in files_data]
        except httpx.HTTPError as e:
            logger.error("Failed to list files", error=str(e))
        except ValidationError as e:
            logger.error("Invalid files response", error=str(e))
        return []

    async def upload(
        self, filename: str, content: bytes, token: str | None = None
    ) -> FileMetadata | None:
        """Upload a file."""
        try:
            response = await self.post(
                "/files",
                files={"file": (filename, content)},
                headers=self._auth_headers(token),
            )
            if response.status_code in (200, 201):
                return FileMetadata.model_validate(response.json())
        except httpx.HTTPError as e:
            logger.error("Failed to upload file", error=str(e))
        except ValidationError as e:
            logger.error("Invalid upload response", error=str(e))
        return None

    async def download(self, file_id: str, token: str | None = None) -> bytes | None:
        """Download a file."""
        try:
            response = await self.get(
                f"/files/{file_id}/download",
                headers=self._auth_headers(token),
            )
            if response.status_code == 200:
                return response.content
        except httpx.HTTPError as e:
            logger.error("Failed to download file", error=str(e))
        return None

    async def remove(self, file_id: str, token: str | None = None) -> bool:
        """Delete a file."""
        try:
            response = await self.request_delete(
                f"/files/{file_id}",
                headers=self._auth_headers(token),
            )
            return response.status_code in (200, 204)
        except httpx.HTTPError as e:
            logger.error("Failed to delete file", error=str(e))
            return False


class GatewayClient(ServiceClient):
    """Client for gateway service interactions."""

    async def check_auth(self, token: str) -> bool:
        """Check if token is accepted by gateway."""
        try:
            response = await self.get(
                "/api/auth/me",
                headers={"Authorization": f"Bearer {token}"},
            )
            return response.status_code == 200
        except httpx.HTTPError as e:
            logger.error("Failed to check auth at gateway", error=str(e))
            return False
