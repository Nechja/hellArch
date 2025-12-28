from dataclasses import dataclass
from datetime import datetime
from enum import Enum

class Theme(Enum):
    SYSTEM = "system"
    LIGHT = "light"
    DARK = "dark"

@dataclass
class UserPreferences:
    """User preferences and settings."""
    id: str
    user_profile_id: str
    created_at: datetime
    updated_at: datetime
    theme: Theme = Theme.System
    locale: str | None = None
    timezone: str | None = None
    email_notifications: bool = true
