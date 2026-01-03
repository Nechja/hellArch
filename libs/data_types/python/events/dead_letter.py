from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class DeadLetter(BaseModel):
    """Dead letter entry for tracking failed events that exhausted retries."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    event_type: str
    event_id: str
    source: str
    original_queue: str
    death_reason: str
    retry_count: int
    body: str
    created_at: datetime
    error_message: str | None = None
