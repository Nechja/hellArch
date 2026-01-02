from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class Webhook(BaseModel):
    """A registered webhook endpoint that receives event notifications."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    url: str
    secret: str
    event_types: list[str]
    created_at: datetime
    updated_at: datetime
    is_active: bool = True
    deliveries: list[WebhookDelivery] = []
