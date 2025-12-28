from dataclasses import dataclass
from datetime import datetime

@dataclass
class Session:
    """User session for cookie-based authentication."""
    id: str
    user_id: str
    token: str
    expires_at: datetime
    created_at: datetime
    ip_address: str | None = None
    user_agent: str | None = None
    user: User | None = None
