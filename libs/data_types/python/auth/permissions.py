from __future__ import annotations


from pydantic import BaseModel, ConfigDict

class Permissions(BaseModel):
    """Centralized permission constants used across all services.
    These are the keys stored in the user's permissions JSONB field.
    """
    model_config = ConfigDict(populate_by_name=True)

