from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class AccessToken(BaseModel):
    """OAuth 2.0 access token (short-lived, 1 hour).
    Stored as SHA-256 hash for security.
    """
    model_config = ConfigDict(populate_by_name=True)

    token_hash: str
    user_id: str
    client_id: str
    scope: str
    expires_at: datetime
    created_at: datetime
    user: IdentityUser | None = None
