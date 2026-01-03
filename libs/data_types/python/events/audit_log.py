from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class AuditLog(BaseModel):
    """Audit log entry for tracking system events."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    event_type: str
    event_id: str
    source: str
    created_at: datetime
    actor_id: str | None = None
    target_id: str | None = None
    metadata: str | None = None
