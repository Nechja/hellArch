from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict

class UserUpdated(BaseModel):
    """Published when user info is updated.
    Event type: hellarch.auth.user.updated.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    user_id: str
    email: str
    username: str
    updated_at: datetime
