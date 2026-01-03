from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class StoredFile(BaseModel):
    """Represents a file stored in the object storage system."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    owner_id: str
    bucket: str
    object_key: str
    filename: str
    mime_type: str
    size_bytes: int
    created_at: datetime
    updated_at: datetime
    checksum_sha256: str | None = None
    metadata: dict[str, str] | None = None
    deleted_at: datetime | None = None
    variants: list[FileVariant] = []
