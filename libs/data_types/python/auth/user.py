from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class User(BaseModel):
    """Core user identity in the auth system."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    email: str
    username: str
    created_at: datetime
    updated_at: datetime
    is_active: bool = True
    permissions: Dictionary<string, bool> | None = None
    sessions: list[Session] = []
    linked_accounts: list[LinkedAccount] = []
