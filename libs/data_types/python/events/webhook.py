from __future__ import annotations

from datetime import datetime
from enum import Enum

from pydantic import BaseModel, ConfigDict

class WebhookFormat(str, Enum):
    RAW = "raw"
    DISCORD = "discord"
    SLACK = "slack"

class Webhook(BaseModel):
    """Webhook endpoint registration for event delivery."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    name: str
    url: str
    secret: str
    created_at: datetime
    updated_at: datetime
    event_types: list[str] = []
    format: WebhookFormat = WebhookFormat.RAW
    is_active: bool = True
    deliveries: list[WebhookDelivery] = []
