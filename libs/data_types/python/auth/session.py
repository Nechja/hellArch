from datetime import datetime
from uuid import UUID


class Session:
    """User session for cookie-based auth"""

    created_at: datetime
    expires_at: datetime
    id: UUID
    token: str
    user_id: UUID

    def __init__(self, created_at: datetime, expires_at: datetime, id: UUID, token: str, user_id: UUID) -> None:
        self.created_at = created_at
        self.expires_at = expires_at
        self.id = id
        self.token = token
        self.user_id = user_id
