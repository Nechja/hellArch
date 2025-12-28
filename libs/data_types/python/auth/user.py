from dataclasses import dataclass
from datetime import datetime

@dataclass
class User:
    """Core user identity in the auth system."""
    id: str
    email: str
    username: str
    created_at: datetime
    updated_at: datetime
    sessions: list[Session] = []
    linked_accounts: list[LinkedAccount] = []
