# Build stage
FROM golang:1.24-alpine AS build
WORKDIR /src

# Install git for go mod download
RUN apk add --no-cache git

# Copy go.mod files for dependency caching
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/go.sum libs/cloud_events/go/
COPY src/services/events/go.mod src/services/events/

WORKDIR /src/src/services/events
RUN go mod download

# Copy source
COPY libs/ /src/libs/
COPY src/services/events/ /src/src/services/events/

# Build
RUN CGO_ENABLED=0 go build -o /app/server ./cmd/server

# Runtime stage
FROM alpine:3.21 AS runtime
RUN apk add --no-cache ca-certificates
COPY --from=build /app/server /usr/local/bin/server
ENTRYPOINT ["/usr/local/bin/server"]

# Development stage with hot reload
FROM golang:1.24-alpine AS dev
WORKDIR /app

RUN apk add --no-cache git && \
    go install github.com/air-verse/air@latest

COPY src/services/events/.air.toml /app/.air.toml

CMD ["air", "-c", ".air.toml"]
