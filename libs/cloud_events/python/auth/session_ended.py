from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

class SessionEndReason(Enum):
    LOGOUT = "logout"
    EXPIRED = "expired"
    REVOKED = "revoked"

@dataclass
class SessionEnded:
    """Published when a session ends (logout or expiry).
    Event type: hellarch.auth.session.ended.v1
    """
    session_id: str
    user_id: str
    reason: SessionEndReason
    ended_at: datetime
