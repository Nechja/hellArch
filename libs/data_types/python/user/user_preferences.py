from enum import Enum
from datetime import datetime
from uuid import UUID
from typing import Optional


class Theme(Enum):
    DARK = "dark"
    LIGHT = "light"
    SYSTEM = "system"


class UserPreferences:
    """User preferences and settings"""

    created_at: datetime
    email_notifications: bool
    id: UUID
    locale: Optional[str]
    theme: Theme
    timezone: Optional[str]
    updated_at: datetime
    user_profile_id: UUID

    def __init__(self, created_at: datetime, email_notifications: bool, id: UUID, locale: Optional[str], theme: Theme, timezone: Optional[str], updated_at: datetime, user_profile_id: UUID) -> None:
        self.created_at = created_at
        self.email_notifications = email_notifications
        self.id = id
        self.locale = locale
        self.theme = theme
        self.timezone = timezone
        self.updated_at = updated_at
        self.user_profile_id = user_profile_id
