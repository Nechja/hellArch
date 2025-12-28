from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict

class AccountLinked(BaseModel):
    """Published when an OAuth account is linked to a user.
    Event type: hellarch.auth.account.linked.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    user_id: str
    provider: str
    provider_user_id: str
    email: str
    linked_at: datetime
