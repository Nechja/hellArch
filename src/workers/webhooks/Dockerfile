FROM golang:1.24-alpine AS build

WORKDIR /repo

# Copy go.mod files for dependency resolution
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/
COPY src/workers/webhooks/go.mod src/workers/webhooks/go.sum src/workers/webhooks/

WORKDIR /repo/src/workers/webhooks
RUN go mod download

WORKDIR /repo

# Copy source
COPY libs/go/ libs/go/
COPY libs/cloud_events/go/ libs/cloud_events/go/
COPY src/workers/webhooks/ src/workers/webhooks/

WORKDIR /repo/src/workers/webhooks
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /webhooks-worker ./cmd/worker

FROM gcr.io/distroless/static AS prod

COPY --from=build /webhooks-worker /webhooks-worker

USER nonroot:nonroot

ENTRYPOINT ["/webhooks-worker"]
