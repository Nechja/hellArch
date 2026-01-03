from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class ServiceRecovered(BaseModel):
    """Published when a service recovers from unhealthy state.
    Event type: hellarch.infra.service.recovered.v1
    """
    model_config = ConfigDict(populate_by_name=True)

    service_name: str
    recovered_at: datetime
    downtime_seconds: int
