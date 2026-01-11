FROM golang:1.24-alpine AS build

WORKDIR /repo

# Copy go.mod files for dependency resolution
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/
COPY src/services/events/go.mod src/services/events/go.sum src/services/events/

WORKDIR /repo/src/services/events
RUN go mod download

WORKDIR /repo

# Copy source
COPY libs/go/ libs/go/
COPY libs/cloud_events/go/ libs/cloud_events/go/
COPY src/services/events/ src/services/events/

WORKDIR /repo/src/services/events
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /events ./cmd/server

FROM gcr.io/distroless/static AS prod

COPY --from=build /events /events

USER nonroot:nonroot

ENTRYPOINT ["/events"]
