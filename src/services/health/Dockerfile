FROM ghcr.io/gleam-lang/gleam:v1.14.0-erlang-alpine AS build

WORKDIR /app

# Copy source files
COPY gleam.toml manifest.toml ./
COPY src ./src

# Build the project
RUN gleam export erlang-shipment

FROM erlang:28-alpine AS runtime

WORKDIR /app

# Copy the built application
COPY --from=build /app/build/erlang-shipment ./

# Set environment variables
ENV PORT=5004

EXPOSE 5004

# Run the application
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["run"]
