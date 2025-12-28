from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class AccountUnlinked(BaseModel):
    """Published when an OAuth account is unlinked from a user.
    Event type: hellarch.auth.account.unlinked.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    user_id: str
    provider: str
    unlinked_at: datetime
