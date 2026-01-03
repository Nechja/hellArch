from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class ServiceUnhealthy(BaseModel):
    """Published when a service becomes unhealthy.
    Event type: hellarch.infra.service.unhealthy.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    service_name: str
    status: str
    consecutive_failures: int
    last_error: Optional[str]
    detected_at: datetime
