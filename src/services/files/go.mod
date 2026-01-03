module github.com/nechja/hellarch/services/files

go 1.24.0

toolchain go1.24.11

require (
	github.com/google/uuid v1.6.0
	github.com/jackc/pgx/v5 v5.8.0
	github.com/minio/minio-go/v7 v7.0.82
	github.com/nechja/hellarch/cloudevents v0.0.0
	github.com/nechja/hellarch/golibs v0.0.0
	go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp v0.60.0
)

replace github.com/nechja/hellarch/cloudevents => ../../../libs/cloud_events/go

replace github.com/nechja/hellarch/golibs => ../../../libs/go
