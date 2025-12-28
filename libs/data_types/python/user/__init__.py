from user.user_profile import UserProfile
from user.user_preferences import UserPreferences, Theme

# Rebuild models to resolve forward references
UserProfile.model_rebuild()
UserPreferences.model_rebuild()

__all__ = ["UserProfile", "UserPreferences", "Theme"]
