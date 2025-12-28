from datetime import datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict

class UserProfile(BaseModel):
    """Extended user profile information.
    Linked to auth user via AuthUserId.
    Email is denormalized from auth for read performance.
    """
    model_config = ConfigDict(populate_by_name=True)

    id: str
    auth_user_id: str
    email: str
    created_at: datetime
    updated_at: datetime
    display_name: str | None = None
    bio: str | None = None
    avatar_url: str | None = None
    preferences: UserPreferences | None = None
