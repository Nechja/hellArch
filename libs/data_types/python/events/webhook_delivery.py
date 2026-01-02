from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class WebhookDelivery(BaseModel):
    """Record of a webhook delivery attempt."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    webhook_id: str
    event_id: str
    event_type: str
    status_code: int
    success: bool
    attempt: int
    created_at: datetime
    payload: str | None = None
    response_body: str | None = None
    webhook: Webhook | None = None
