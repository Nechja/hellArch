FROM golang:1.24-alpine AS build

WORKDIR /repo

# Copy go.mod files for dependency resolution
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/
COPY src/workers/audit/go.mod src/workers/audit/go.sum src/workers/audit/

WORKDIR /repo/src/workers/audit
RUN go mod download

WORKDIR /repo

# Copy source
COPY libs/go/ libs/go/
COPY libs/cloud_events/go/ libs/cloud_events/go/
COPY src/workers/audit/ src/workers/audit/

WORKDIR /repo/src/workers/audit
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /audit-worker ./cmd/worker

FROM gcr.io/distroless/static AS prod

COPY --from=build /audit-worker /audit-worker

USER nonroot:nonroot

ENTRYPOINT ["/audit-worker"]
