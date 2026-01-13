FROM python:3.12-slim AS base

WORKDIR /app

# Install system dependencies for Playwright browsers
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 \
    libxrandr2 libgbm1 libasound2 libpango-1.0-0 libcairo2 \
    libdbus-1-3 libatspi2.0-0 libx11-6 libxcb1 libxext6 \
    && rm -rf /var/lib/apt/lists/*

# Install uv for fast package management
COPY --from=ghcr.io/astral-sh/uv:0.5.14 /uv /usr/local/bin/uv

ENV UV_COMPILE_BYTECODE=1
ENV UV_LINK_MODE=copy
ENV PYTHONUNBUFFERED=1

FROM base AS build

# Copy project files
COPY pyproject.toml ./
COPY src ./src

# Install dependencies and Playwright browser
RUN uv pip install --system -e . && \
    playwright install chromium

FROM base AS prod

# Copy installed packages and source
COPY --from=build /usr/local/lib/python3.12/site-packages /usr/local/lib/python3.12/site-packages
COPY --from=build /app/src ./src
COPY pyproject.toml ./

# Copy Playwright browser cache
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Set Python path
ENV PYTHONPATH=/app/src

# Default command runs the API server
CMD ["python", "-m", "uvicorn", "testing.api:app", "--host", "0.0.0.0", "--port", "5007"]
