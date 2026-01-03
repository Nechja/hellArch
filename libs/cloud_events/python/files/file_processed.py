from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class FileProcessed(BaseModel):
    """Published when file processing completes (e.g., thumbnail generation).
    Event type: hellarch.files.file.processed.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    file_id: str
    variant_type: str
    status: str
    error: Optional[str]
    processed_at: datetime
