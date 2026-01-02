package rabbitmq

import amqp "github.com/rabbitmq/amqp091-go"

// AMQPCarrier implements propagation.TextMapCarrier for AMQP headers.
// Used to propagate trace context through RabbitMQ messages.
type AMQPCarrier amqp.Table

func (c AMQPCarrier) Get(key string) string {
	if v, ok := c[key]; ok {
		if s, ok := v.(string); ok {
			return s
		}
	}
	return ""
}

func (c AMQPCarrier) Set(key, value string) {
	c[key] = value
}

func (c AMQPCarrier) Keys() []string {
	keys := make([]string, 0, len(c))
	for k := range c {
		keys = append(keys, k)
	}
	return keys
}

// ExtractHeader extracts a string value from AMQP headers.
func ExtractHeader(headers amqp.Table, key string) string {
	if v, ok := headers[key]; ok {
		if s, ok := v.(string); ok {
			return s
		}
	}
	return ""
}
