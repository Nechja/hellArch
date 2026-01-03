package httpclient

import (
	"net/http"
	"time"

	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

func New(timeout time.Duration) *http.Client {
	return &http.Client{
		Transport: otelhttp.NewTransport(http.DefaultTransport),
		Timeout:   timeout,
	}
}

func NewDefault() *http.Client {
	return New(10 * time.Second)
}
