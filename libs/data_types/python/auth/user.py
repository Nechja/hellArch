from datetime import datetime
from uuid import UUID


class User:
    """Core user identity"""

    created_at: datetime
    email: str
    id: UUID
    updated_at: datetime
    username: str

    def __init__(self, created_at: datetime, email: str, id: UUID, updated_at: datetime, username: str) -> None:
        self.created_at = created_at
        self.email = email
        self.id = id
        self.updated_at = updated_at
        self.username = username
