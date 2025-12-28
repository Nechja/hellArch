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
    sessions: ICollection<Session> = []
    linked_accounts: ICollection<LinkedAccount> = []
