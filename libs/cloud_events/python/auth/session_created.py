from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict

class SessionCreated(BaseModel):
    """Published when a new session is created.
    Event type: hellarch.auth.session.created.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    session_id: str
    user_id: str
    ip_address: str
    user_agent: str
    created_at: datetime
    expires_at: datetime
