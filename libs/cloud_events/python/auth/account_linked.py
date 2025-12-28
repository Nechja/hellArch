from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

@dataclass
class AccountLinked:
    """Published when an OAuth account is linked to a user.
    Event type: hellarch.auth.account.linked.v1
    """
    user_id: str
    provider: str
    provider_user_id: str
    email: str
    linked_at: datetime
