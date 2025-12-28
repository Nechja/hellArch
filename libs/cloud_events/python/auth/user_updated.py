from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

@dataclass
class UserUpdated:
    """Published when user info is updated.
    Event type: hellarch.auth.user.updated.v1
    """
    user_id: str
    email: str
    username: str
    updated_at: datetime
