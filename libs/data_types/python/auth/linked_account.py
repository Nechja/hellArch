from dataclasses import dataclass
from datetime import datetime

@dataclass
class LinkedAccount:
    """OAuth provider account linked to a user.
Stores provider-specific identity info. Token storage is optional
(only needed if we want to call provider APIs on behalf of user)."""
    id: str
    user_id: str
    provider: str
    provider_user_id: str
    created_at: datetime
    updated_at: datetime
    email: str | None = None
    username: str | None = None
    avatar_url: str | None = None
    access_token: str | None = None
    refresh_token: str | None = None
    token_expires_at: datetime | None = None
