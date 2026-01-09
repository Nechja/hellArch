from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class RefreshToken(BaseModel):
    """OAuth 2.0 refresh token (long-lived, 30 days).
    Stored as SHA-256 hash for security.
    Supports token rotation with reuse detection.
    """
    model_config = ConfigDict(populate_by_name=True)

    token_hash: str
    user_id: str
    client_id: str
    scope: str
    expires_at: datetime
    created_at: datetime
    family_id: str
    is_revoked: bool
    client_fingerprint: str | None = None
    user: IdentityUser | None = None
