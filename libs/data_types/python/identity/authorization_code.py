from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class AuthorizationCode(BaseModel):
    """OAuth 2.0 authorization code grant (short-lived, 5 minutes)."""
    model_config = ConfigDict(populate_by_name=True)

    code: str
    client_id: str
    user_id: str
    redirect_uri: str
    scope: str
    expires_at: datetime
    created_at: datetime
    code_challenge: str | None = None
    code_challenge_method: str | None = None
    user: IdentityUser | None = None
