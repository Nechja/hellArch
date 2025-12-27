from uuid import UUID
from typing import Optional
from datetime import datetime


class UserProfile:
    """Extended user profile info"""

    auth_user_id: UUID
    avatar_url: Optional[str]
    bio: Optional[str]
    created_at: datetime
    display_name: Optional[str]
    id: UUID
    updated_at: datetime

    def __init__(self, auth_user_id: UUID, avatar_url: Optional[str], bio: Optional[str], created_at: datetime, display_name: Optional[str], id: UUID, updated_at: datetime) -> None:
        self.auth_user_id = auth_user_id
        self.avatar_url = avatar_url
        self.bio = bio
        self.created_at = created_at
        self.display_name = display_name
        self.id = id
        self.updated_at = updated_at
