FROM golang:1.24-alpine AS dev

WORKDIR /repo

RUN go install github.com/air-verse/air@v1.62.0
RUN mkdir -p /.cache/go-build /go/pkg/mod && chmod -R 777 /.cache /go

COPY . .

WORKDIR /repo/src/services/auth
CMD ["air", "-c", ".air.toml"]

FROM golang:1.24-alpine AS build

WORKDIR /repo

# Copy go.mod files for dependency resolution
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/data_types/go/go.mod libs/data_types/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/
COPY src/services/auth/go.mod src/services/auth/go.sum src/services/auth/

WORKDIR /repo/src/services/auth
RUN go mod download

WORKDIR /repo

# Copy source
COPY libs/go/ libs/go/
COPY libs/data_types/go/ libs/data_types/go/
COPY libs/cloud_events/go/ libs/cloud_events/go/
COPY src/services/auth/ src/services/auth/

WORKDIR /repo/src/services/auth
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /auth ./cmd/server

FROM gcr.io/distroless/static AS prod

COPY --from=build /auth /auth

USER nonroot:nonroot

ENTRYPOINT ["/auth"]
