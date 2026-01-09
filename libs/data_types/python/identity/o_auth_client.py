from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class OAuthClient(BaseModel):
    """OAuth 2.0 client application registration."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    secret_hash: str
    name: str
    redirect_uris: list[str]
    created_at: datetime
    is_active: bool = True
