FROM python:3.12-slim AS base

WORKDIR /app

# Install uv for fast package management
COPY --from=ghcr.io/astral-sh/uv:0.5.14 /uv /usr/local/bin/uv

ENV UV_COMPILE_BYTECODE=1
ENV UV_LINK_MODE=copy
ENV PYTHONUNBUFFERED=1

FROM base AS build

# Copy project files
COPY pyproject.toml ./
COPY src ./src

# Install dependencies
RUN uv pip install --system -e .

FROM base AS prod

# Copy installed packages and source
COPY --from=build /usr/local/lib/python3.12/site-packages /usr/local/lib/python3.12/site-packages
COPY --from=build /app/src ./src
COPY pyproject.toml ./

# Set Python path
ENV PYTHONPATH=/app/src

# Default command runs the API server
CMD ["python", "-m", "uvicorn", "testing.api:app", "--host", "0.0.0.0", "--port", "5007"]
