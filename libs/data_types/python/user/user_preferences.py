from __future__ import annotations

from datetime import datetime
from enum import Enum

from pydantic import BaseModel, ConfigDict

class Theme(str, Enum):
    SYSTEM = "system"
    LIGHT = "light"
    DARK = "dark"

class UserPreferences(BaseModel):
    """User preferences and settings."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    user_profile_id: str
    created_at: datetime
    updated_at: datetime
    theme: Theme = Theme.SYSTEM
    locale: str | None = None
    timezone: str | None = None
    email_notifications: bool = True
    push_notifications: bool = True
    in_app_notifications: bool = True
    user_profile: UserProfile | None = None
