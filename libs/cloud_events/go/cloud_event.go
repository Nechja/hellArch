package cloudevents

import "time"

type CloudEvent[T any] struct {
	SpecVersion     string            `json:"specversion"`
	ID              string            `json:"id"`
	Source          string            `json:"source"`
	Type            string            `json:"type"`
	DataContentType string            `json:"datacontenttype"`
	Time            time.Time         `json:"time"`
	Data            T                 `json:"data"`
	Extensions      map[string]string `json:"extensions,omitempty"`
}
