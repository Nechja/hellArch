from datetime import datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict

class Session(BaseModel):
    """User session for cookie-based authentication."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    user_id: str
    token: str
    expires_at: datetime
    created_at: datetime
    ip_address: str | None = None
    user_agent: str | None = None
    user: User | None = None
