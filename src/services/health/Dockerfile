# Build stage
FROM ghcr.io/gleam-lang/gleam:v1.14.0-erlang-alpine AS builder

WORKDIR /app

# Copy source files
COPY gleam.toml manifest.toml ./
COPY src ./src

# Build the project
RUN gleam export erlang-shipment

# Runtime stage
FROM erlang:27-alpine AS runtime

WORKDIR /app

# Copy the built application
COPY --from=builder /app/build/erlang-shipment ./

# Set environment variables
ENV PORT=5004

EXPOSE 5004

# Run the application
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["run"]

# Development stage with hot reload
FROM ghcr.io/gleam-lang/gleam:v1.14.0-erlang-alpine AS dev

WORKDIR /app

# Install inotify-tools for file watching
RUN apk add --no-cache inotify-tools bash

# Copy source files
COPY gleam.toml manifest.toml ./
COPY src ./src

# Download dependencies
RUN gleam deps download

# Set environment variables
ENV PORT=5004

EXPOSE 5004

# Run with watch mode (simple restart on changes)
CMD ["sh", "-c", "gleam run"]
