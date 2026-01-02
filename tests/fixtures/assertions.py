from datetime import datetime
from typing import Any, TypeVar

import httpx
from pydantic import BaseModel, ValidationError

T = TypeVar("T", bound=BaseModel)


class TestAssertionError(AssertionError):
    """Custom assertion error with detailed messages for test failures."""

    pass


def assert_status(response: httpx.Response, expected: int, message: str | None = None) -> None:
    """Assert response status code with detailed error message."""
    if response.status_code != expected:
        body = response.text[:500] if response.text else "(empty)"
        msg = message or f"Expected status {expected}, got {response.status_code}"
        raise TestAssertionError(f"{msg}\nResponse body: {body}")


def assert_json_contains(response: httpx.Response, expected: dict[str, Any]) -> None:
    """Assert that response JSON contains expected key-value pairs."""
    data = response.json()
    for key, value in expected.items():
        if key not in data:
            raise TestAssertionError(f"Missing key '{key}' in response: {data}")
        if data[key] != value:
            raise TestAssertionError(f"Expected {key}={value!r}, got {data[key]!r}")


def assert_json_structure(response: httpx.Response, keys: list[str]) -> None:
    """Assert that response JSON has all expected keys."""
    data = response.json()
    missing = [k for k in keys if k not in data]
    if missing:
        raise TestAssertionError(f"Missing keys in response: {missing}\nGot: {list(data.keys())}")


def assert_error_response(
    response: httpx.Response,
    expected_status: int,
    error_key: str = "error",
    expected_error: str | None = None,
) -> None:
    """Assert an error response with optional error message validation."""
    assert_status(response, expected_status)
    data = response.json()
    if error_key not in data:
        raise TestAssertionError(f"Expected error key '{error_key}' in response: {data}")
    if expected_error and data[error_key] != expected_error:
        raise TestAssertionError(f"Expected error '{expected_error}', got '{data[error_key]}'")


def assert_valid_uuid(value: str, field_name: str = "value") -> None:
    """Assert that a string is a valid UUID."""
    import re

    uuid_pattern = re.compile(
        r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        re.IGNORECASE,
    )
    if not uuid_pattern.match(value):
        raise TestAssertionError(f"Invalid UUID for {field_name}: {value}")


def assert_valid_datetime(value: str, field_name: str = "value") -> datetime:
    """Assert that a string is a valid ISO datetime and return parsed datetime."""
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (ValueError, AttributeError) as e:
        raise TestAssertionError(f"Invalid datetime for {field_name}: {value}") from e


def assert_response_model(response: httpx.Response, model: type[T]) -> T:
    """Validate response JSON against a Pydantic model and return the parsed instance."""
    try:
        data = response.json()
        return model.model_validate(data)
    except ValidationError as e:
        raise TestAssertionError(f"Response does not match {model.__name__}:\n{e}") from e


def assert_list_response(
    response: httpx.Response,
    item_model: type[T],
    min_items: int = 0,
    max_items: int | None = None,
) -> list[T]:
    """Validate a list response against a Pydantic model."""
    data = response.json()
    if not isinstance(data, list):
        raise TestAssertionError(f"Expected list response, got {type(data).__name__}")

    if len(data) < min_items:
        raise TestAssertionError(f"Expected at least {min_items} items, got {len(data)}")

    if max_items is not None and len(data) > max_items:
        raise TestAssertionError(f"Expected at most {max_items} items, got {len(data)}")

    items = []
    for i, item in enumerate(data):
        try:
            items.append(item_model.model_validate(item))
        except ValidationError as e:
            raise TestAssertionError(f"Item {i} does not match {item_model.__name__}:\n{e}") from e

    return items


def assert_pagination_response(
    response: httpx.Response,
    item_model: type[T],
    items_key: str = "items",
    total_key: str = "total",
    page_key: str = "page",
    per_page_key: str = "per_page",
) -> tuple[list[T], dict[str, int]]:
    """Validate a paginated response and return items with pagination metadata."""
    data = response.json()

    required_keys = [items_key, total_key]
    missing = [k for k in required_keys if k not in data]
    if missing:
        raise TestAssertionError(f"Missing pagination keys: {missing}")

    items_data = data[items_key]
    if not isinstance(items_data, list):
        raise TestAssertionError(f"Expected {items_key} to be a list")

    items = []
    for i, item in enumerate(items_data):
        try:
            items.append(item_model.model_validate(item))
        except ValidationError as e:
            raise TestAssertionError(f"Item {i} does not match {item_model.__name__}:\n{e}") from e

    pagination = {
        "total": data[total_key],
        "page": data.get(page_key, 1),
        "per_page": data.get(per_page_key, len(items_data)),
    }

    return items, pagination


class ResponseAssertions:
    """Chainable response assertions for fluent test syntax."""

    def __init__(self, response: httpx.Response):
        self.response = response
        self._data: dict[str, Any] | None = None

    @property
    def data(self) -> dict[str, Any]:
        if self._data is None:
            self._data = self.response.json()
        return self._data

    def status(self, expected: int) -> "ResponseAssertions":
        assert_status(self.response, expected)
        return self

    def contains(self, **kwargs: Any) -> "ResponseAssertions":
        assert_json_contains(self.response, kwargs)
        return self

    def has_keys(self, *keys: str) -> "ResponseAssertions":
        assert_json_structure(self.response, list(keys))
        return self

    def matches(self, model: type[T]) -> T:
        return assert_response_model(self.response, model)

    def is_error(self, expected_error: str | None = None) -> "ResponseAssertions":
        if "error" not in self.data:
            raise TestAssertionError(f"Expected error response, got: {self.data}")
        if expected_error and self.data["error"] != expected_error:
            raise TestAssertionError(f"Expected error '{expected_error}', got '{self.data['error']}'")
        return self


def assert_response(response: httpx.Response) -> ResponseAssertions:
    """Create a chainable response assertion object."""
    return ResponseAssertions(response)
