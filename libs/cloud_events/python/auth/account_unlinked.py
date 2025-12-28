from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional

@dataclass
class AccountUnlinked:
    """Published when an OAuth account is unlinked from a user.
    Event type: hellarch.auth.account.unlinked.v1
    """
    user_id: str
    provider: str
    unlinked_at: datetime
