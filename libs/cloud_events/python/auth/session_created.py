from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

@dataclass
class SessionCreated:
    """Published when a new session is created.
    Event type: hellarch.auth.session.created.v1
    """
    session_id: str
    user_id: str
    ip_address: str
    user_agent: str
    created_at: datetime
    expires_at: datetime
