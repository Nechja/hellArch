FROM rust:1.85-alpine AS dev

WORKDIR /app

RUN apk add --no-cache musl-dev

COPY Cargo.toml Cargo.lock ./
COPY src ./src

RUN cargo build

CMD ["./target/debug/events-worker"]

FROM rust:1.85-alpine AS build

WORKDIR /app

RUN apk add --no-cache musl-dev

COPY Cargo.toml Cargo.lock ./
COPY src ./src

RUN cargo build --release

FROM alpine:3.21 AS prod

RUN apk add --no-cache ca-certificates

COPY --from=build /app/target/release/events-worker /usr/local/bin/

USER nobody:nobody

ENTRYPOINT ["events-worker"]
