from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class UserCreated(BaseModel):
    """Published when a new user is created via OAuth.
    Event type: hellarch.auth.user.created.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    user_id: str
    email: str
    username: str
    provider: str
    provider_user_id: str
    avatar_url: str
    created_at: datetime
