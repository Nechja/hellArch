from __future__ import annotations


from pydantic import BaseModel, ConfigDict

class Admin(BaseModel):
    """Admin panel and administrative functions"""
    model_config = ConfigDict(populate_by_name=True)

