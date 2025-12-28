from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class UserDeleted(BaseModel):
    """Published when a user is deleted.
    Event type: hellarch.auth.user.deleted.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    user_id: str
    deleted_at: datetime
