from typing import Optional
from datetime import datetime
from uuid import UUID


class LinkedAccount:
    """OAuth provider linked to a user"""

    access_token: Optional[str]
    avatar_url: Optional[str]
    created_at: datetime
    email: Optional[str]
    expires_at: Optional[datetime]
    id: UUID
    provider: str
    provider_user_id: str
    refresh_token: Optional[str]
    updated_at: datetime
    user_id: UUID
    username: Optional[str]

    def __init__(self, access_token: Optional[str], avatar_url: Optional[str], created_at: datetime, email: Optional[str], expires_at: Optional[datetime], id: UUID, provider: str, provider_user_id: str, refresh_token: Optional[str], updated_at: datetime, user_id: UUID, username: Optional[str]) -> None:
        self.access_token = access_token
        self.avatar_url = avatar_url
        self.created_at = created_at
        self.email = email
        self.expires_at = expires_at
        self.id = id
        self.provider = provider
        self.provider_user_id = provider_user_id
        self.refresh_token = refresh_token
        self.updated_at = updated_at
        self.user_id = user_id
        self.username = username
