from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

@dataclass
class UserDeleted:
    """Published when a user is deleted.
    Event type: hellarch.auth.user.deleted.v1
    """
    user_id: str
    deleted_at: datetime
