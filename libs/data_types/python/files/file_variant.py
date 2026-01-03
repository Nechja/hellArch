from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class FileVariant(BaseModel):
    """Represents a processed variant of a stored file (e.g., thumbnail, optimized version)."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    file_id: str
    variant_type: str
    object_key: str
    mime_type: str
    size_bytes: int
    created_at: datetime
    file: StoredFile | None = None
