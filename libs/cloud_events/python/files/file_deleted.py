from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class FileDeleted(BaseModel):
    """Published when a file is deleted.
    Event type: hellarch.files.file.deleted.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    file_id: str
    filename: str
    owner_id: str
    deleted_at: datetime
