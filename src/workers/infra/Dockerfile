# Build stage
FROM golang:1.24-alpine AS builder

WORKDIR /build

# Copy go mod files
COPY go.mod go.sum* ./

# Copy shared libs for local replace directives
COPY ../../../libs/cloud_events/go /libs/cloud_events/go

# Download dependencies
RUN go mod download

# Copy source
COPY . .

# Build
RUN CGO_ENABLED=0 GOOS=linux go build -o infra-worker .

# Runtime stage
FROM alpine:3.21 AS runtime

RUN apk add --no-cache ca-certificates

WORKDIR /app

COPY --from=builder /build/infra-worker .

ENTRYPOINT ["/app/infra-worker"]

# Development stage
FROM golang:1.24-alpine AS dev

WORKDIR /app

RUN apk add --no-cache git

# Install air for hot reload
RUN go install github.com/air-verse/air@latest

COPY go.mod go.sum* ./

ENV GO111MODULE=on

CMD ["air", "-c", ".air.toml"]
