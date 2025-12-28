from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict

class SessionEndReason(str, Enum):
    LOGOUT = "logout"
    EXPIRED = "expired"
    REVOKED = "revoked"

class SessionEnded(BaseModel):
    """Published when a session ends (logout or expiry).
    Event type: hellarch.auth.session.ended.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    session_id: str
    user_id: str
    reason: SessionEndReason
    ended_at: datetime
