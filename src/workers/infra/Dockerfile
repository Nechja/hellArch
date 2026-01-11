FROM golang:1.24-alpine AS dev

WORKDIR /repo

RUN go install github.com/air-verse/air@v1.62.0
RUN mkdir -p /.cache/go-build /go/pkg/mod && chmod -R 777 /.cache /go

COPY . .

WORKDIR /repo/src/workers/infra
CMD ["air", "-c", ".air.toml"]

FROM golang:1.24-alpine AS build

WORKDIR /repo

# Copy go.mod files for dependency resolution
COPY libs/go/go.mod libs/go/go.sum libs/go/
COPY libs/cloud_events/go/go.mod libs/cloud_events/go/
COPY src/workers/infra/go.mod src/workers/infra/go.sum src/workers/infra/

WORKDIR /repo/src/workers/infra
RUN go mod download

WORKDIR /repo

# Copy source
COPY libs/go/ libs/go/
COPY libs/cloud_events/go/ libs/cloud_events/go/
COPY src/workers/infra/ src/workers/infra/

WORKDIR /repo/src/workers/infra
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /infra-worker .

FROM gcr.io/distroless/static AS prod

COPY --from=build /infra-worker /infra-worker

USER nonroot:nonroot

ENTRYPOINT ["/infra-worker"]
