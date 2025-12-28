FROM golang:1.24-alpine AS dev

WORKDIR /app

# Hot reload with air
RUN go install github.com/air-verse/air@v1.62.0

# Create cache directories with proper permissions
RUN mkdir -p /.cache/go-build /go/pkg/mod && chmod -R 777 /.cache /go

COPY go.mod go.sum ./
RUN go mod download

COPY . .

ENTRYPOINT ["air", "-c", ".air.toml"]
