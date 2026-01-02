from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class AuditLog(BaseModel):
    """Stores a record of every event that passes through the system for audit purposes."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    event_type: str
    event_id: str
    source: str
    created_at: datetime
    actor_id: str | None = None
    target_id: str | None = None
    metadata: str | None = None
