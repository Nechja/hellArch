FROM golang:1.24-alpine AS dev

WORKDIR /app

RUN go install github.com/air-verse/air@v1.62.0
RUN mkdir -p /.cache/go-build /go/pkg/mod && chmod -R 777 /.cache /go

COPY . .

CMD ["air", "-c", ".air.toml"]

FROM golang:1.24-alpine AS build

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o /auth ./cmd/server

FROM gcr.io/distroless/static AS prod

COPY --from=build /auth /auth

USER nonroot:nonroot

ENTRYPOINT ["/auth"]
