from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class FileUploaded(BaseModel):
    """Published when a file is successfully uploaded.
    Event type: hellarch.files.file.uploaded.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    file_id: str
    filename: str
    mime_type: str
    size_bytes: int
    owner_id: str
    checksum_sha256: Optional[str]
    uploaded_at: datetime
