from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class IdentityUser(BaseModel):
    """Local identity user with username/password authentication."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    email: str
    username: str
    password_hash: str
    created_at: datetime
    updated_at: datetime
    is_active: bool = True
    refresh_tokens: list[RefreshToken] = []
