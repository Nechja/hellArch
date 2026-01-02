from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict

class WebhookDelivery(BaseModel):
    """Tracks each attempt to deliver an event to a webhook endpoint."""
    model_config = ConfigDict(populate_by_name=True)

    id: str
    webhook_id: str
    event_id: str
    event_type: str
    payload: str
    attempt_number: int
    created_at: datetime
    status_code: int | None = None
    response_body: str | None = None
    error: str | None = None
    webhook: Webhook = null!
