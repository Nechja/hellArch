"""
API response schemas for contract testing.

These schemas define the expected structure of API responses.
Contract tests validate that actual responses match these schemas.
"""

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Any
import re


class FieldType(Enum):
    """Supported field types for schema validation."""

    STRING = "string"
    INT = "int"
    FLOAT = "float"
    BOOL = "bool"
    LIST = "list"
    DICT = "dict"
    DATETIME = "datetime"
    UUID = "uuid"
    ANY = "any"
    NULL = "null"


@dataclass
class SchemaField:
    """Definition of a single field in a schema."""

    name: str
    field_type: FieldType
    required: bool = True
    nullable: bool = False
    items_type: FieldType | None = None
    nested_schema: "Schema | None" = None


@dataclass
class Schema:
    """Definition of an API response schema."""

    name: str
    fields: list[SchemaField] = field(default_factory=list)

    def validate(self, data: dict[str, Any]) -> list[str]:
        """Validate data against this schema. Returns list of errors."""
        errors = []

        if not isinstance(data, dict):
            return [f"Expected dict, got {type(data).__name__}"]

        for schema_field in self.fields:
            if schema_field.required and schema_field.name not in data:
                errors.append(f"Missing required field: {schema_field.name}")
                continue

            if schema_field.name not in data:
                continue

            value = data[schema_field.name]

            if value is None:
                if not schema_field.nullable:
                    errors.append(f"Field {schema_field.name} is null but not nullable")
                continue

            type_error = self._validate_type(
                schema_field.name,
                value,
                schema_field.field_type,
                schema_field.items_type,
                schema_field.nested_schema,
            )
            if type_error:
                errors.append(type_error)

        return errors

    def _validate_type(
        self,
        field_name: str,
        value: Any,
        expected_type: FieldType,
        items_type: FieldType | None,
        nested_schema: "Schema | None",
    ) -> str | None:
        """Validate a single field's type."""
        if expected_type == FieldType.ANY:
            return None

        if expected_type == FieldType.STRING:
            if not isinstance(value, str):
                return f"Field {field_name}: expected string, got {type(value).__name__}"

        elif expected_type == FieldType.INT:
            if not isinstance(value, int) or isinstance(value, bool):
                return f"Field {field_name}: expected int, got {type(value).__name__}"

        elif expected_type == FieldType.FLOAT:
            if not isinstance(value, (int, float)) or isinstance(value, bool):
                return f"Field {field_name}: expected float, got {type(value).__name__}"

        elif expected_type == FieldType.BOOL:
            if not isinstance(value, bool):
                return f"Field {field_name}: expected bool, got {type(value).__name__}"

        elif expected_type == FieldType.LIST:
            if not isinstance(value, list):
                return f"Field {field_name}: expected list, got {type(value).__name__}"
            if items_type and value:
                for i, item in enumerate(value):
                    if nested_schema and items_type == FieldType.DICT:
                        errors = nested_schema.validate(item)
                        if errors:
                            return f"Field {field_name}[{i}]: {errors[0]}"

        elif expected_type == FieldType.DICT:
            if not isinstance(value, dict):
                return f"Field {field_name}: expected dict, got {type(value).__name__}"
            if nested_schema:
                errors = nested_schema.validate(value)
                if errors:
                    return f"Field {field_name}: {errors[0]}"

        elif expected_type == FieldType.DATETIME:
            if not isinstance(value, str):
                return f"Field {field_name}: expected datetime string, got {type(value).__name__}"
            try:
                datetime.fromisoformat(value.replace("Z", "+00:00"))
            except ValueError:
                return f"Field {field_name}: invalid datetime format"

        elif expected_type == FieldType.UUID:
            if not isinstance(value, str):
                return f"Field {field_name}: expected UUID string, got {type(value).__name__}"
            uuid_pattern = r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"
            if not re.match(uuid_pattern, value.lower()):
                return f"Field {field_name}: invalid UUID format"

        return None


# =============================================================================
# Auth Service Schemas
# =============================================================================

AUTH_HEALTH_SCHEMA = Schema(
    name="AuthHealth",
    fields=[SchemaField("status", FieldType.STRING)],
)

AUTH_VALIDATE_RESPONSE = Schema(
    name="AuthValidateResponse",
    fields=[
        SchemaField("valid", FieldType.BOOL),
        SchemaField("user_id", FieldType.STRING, required=False, nullable=True),
        SchemaField("email", FieldType.STRING, required=False, nullable=True),
        SchemaField("username", FieldType.STRING, required=False, nullable=True),
        SchemaField("error", FieldType.STRING, required=False, nullable=True),
    ],
)

AUTH_REFRESH_RESPONSE = Schema(
    name="AuthRefreshResponse",
    fields=[
        SchemaField("access_token", FieldType.STRING),
        SchemaField("refresh_token", FieldType.STRING),
        SchemaField("expires_in", FieldType.INT),
        SchemaField("token_type", FieldType.STRING, required=False),
    ],
)

AUTH_ME_RESPONSE = Schema(
    name="AuthMeResponse",
    fields=[
        SchemaField("user_id", FieldType.STRING),
        SchemaField("email", FieldType.STRING, required=False, nullable=True),
        SchemaField("username", FieldType.STRING, required=False, nullable=True),
    ],
)

AUTH_PROVIDERS_RESPONSE = Schema(
    name="AuthProvidersResponse",
    fields=[
        SchemaField("providers", FieldType.LIST, items_type=FieldType.STRING),
    ],
)

# =============================================================================
# Events Service Schemas
# =============================================================================

AUDIT_LOG_SCHEMA = Schema(
    name="AuditLog",
    fields=[
        SchemaField("id", FieldType.UUID),
        SchemaField("event_id", FieldType.STRING),
        SchemaField("event_type", FieldType.STRING),
        SchemaField("source", FieldType.STRING),
        SchemaField("actor_id", FieldType.STRING, required=False, nullable=True),
        SchemaField("target_id", FieldType.STRING, required=False, nullable=True),
        SchemaField("metadata", FieldType.DICT, required=False, nullable=True),
        SchemaField("created_at", FieldType.DATETIME),
    ],
)

AUDIT_LOGS_RESPONSE = Schema(
    name="AuditLogsResponse",
    fields=[
        SchemaField("data", FieldType.LIST, items_type=FieldType.DICT, nested_schema=AUDIT_LOG_SCHEMA),
        SchemaField("total", FieldType.INT, required=False),
    ],
)

WEBHOOK_SCHEMA = Schema(
    name="Webhook",
    fields=[
        SchemaField("id", FieldType.UUID),
        SchemaField("name", FieldType.STRING),
        SchemaField("url", FieldType.STRING),
        SchemaField("event_types", FieldType.LIST, items_type=FieldType.STRING),
        SchemaField("is_active", FieldType.BOOL),
        SchemaField("created_at", FieldType.DATETIME),
        SchemaField("updated_at", FieldType.DATETIME),
    ],
)

WEBHOOKS_LIST_RESPONSE = Schema(
    name="WebhooksListResponse",
    fields=[
        SchemaField("data", FieldType.LIST),
        SchemaField("total", FieldType.INT, required=False),
    ],
)

# =============================================================================
# Files Service Schemas
# =============================================================================

FILE_METADATA_SCHEMA = Schema(
    name="FileMetadata",
    fields=[
        SchemaField("id", FieldType.UUID),
        SchemaField("filename", FieldType.STRING),
        SchemaField("content_type", FieldType.STRING, required=False),
        SchemaField("size", FieldType.INT, required=False),
        SchemaField("created_at", FieldType.DATETIME, required=False),
    ],
)

FILE_UPLOAD_RESPONSE = Schema(
    name="FileUploadResponse",
    fields=[
        SchemaField("id", FieldType.UUID),
        SchemaField("filename", FieldType.STRING, required=False),
    ],
)

FILES_LIST_RESPONSE = Schema(
    name="FilesListResponse",
    fields=[
        SchemaField("files", FieldType.LIST),
        SchemaField("limit", FieldType.INT, required=False),
        SchemaField("offset", FieldType.INT, required=False),
    ],
)

# =============================================================================
# CloudEvents Schema
# =============================================================================

CLOUD_EVENT_SCHEMA = Schema(
    name="CloudEvent",
    fields=[
        SchemaField("specversion", FieldType.STRING),
        SchemaField("type", FieldType.STRING),
        SchemaField("source", FieldType.STRING),
        SchemaField("id", FieldType.STRING),
        SchemaField("time", FieldType.DATETIME, required=False),
        SchemaField("data", FieldType.ANY, required=False, nullable=True),
    ],
)

# =============================================================================
# Generic Schemas
# =============================================================================

HEALTH_SCHEMA = Schema(
    name="Health",
    fields=[SchemaField("status", FieldType.STRING)],
)

USER_HEALTH_SCHEMA = Schema(
    name="UserHealth",
    fields=[
        SchemaField("status", FieldType.STRING),
        SchemaField("database", FieldType.BOOL, required=False),
    ],
)

PAGINATED_RESPONSE = Schema(
    name="PaginatedResponse",
    fields=[
        SchemaField("data", FieldType.LIST),
        SchemaField("total", FieldType.INT, required=False),
        SchemaField("page", FieldType.INT, required=False),
        SchemaField("page_size", FieldType.INT, required=False),
    ],
)

# =============================================================================
# Schema Registry - maps schema names to schema objects
# =============================================================================

SCHEMAS: dict[str, Schema] = {
    "health": HEALTH_SCHEMA,
    "auth_health": AUTH_HEALTH_SCHEMA,
    "auth_validate": AUTH_VALIDATE_RESPONSE,
    "auth_refresh": AUTH_REFRESH_RESPONSE,
    "auth_me": AUTH_ME_RESPONSE,
    "auth_providers": AUTH_PROVIDERS_RESPONSE,
    "audit_log": AUDIT_LOG_SCHEMA,
    "audit_logs": AUDIT_LOGS_RESPONSE,
    "webhook": WEBHOOK_SCHEMA,
    "webhooks_list": WEBHOOKS_LIST_RESPONSE,
    "file_metadata": FILE_METADATA_SCHEMA,
    "file_upload": FILE_UPLOAD_RESPONSE,
    "files_list": FILES_LIST_RESPONSE,
    "user_health": USER_HEALTH_SCHEMA,
    "cloud_event": CLOUD_EVENT_SCHEMA,
    "paginated": PAGINATED_RESPONSE,
}
