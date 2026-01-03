package consumer

import (
	"context"
	"encoding/json"
	"log/slog"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/workers/images/internal/processor"
)

type Consumer struct {
	consumer  *rabbitmq.Consumer
	processor *processor.Processor
}

type Config struct {
	RabbitMQURL string
	Processor   *processor.Processor
}

func New(ctx context.Context, cfg Config) (*Consumer, error) {
	consumer, err := rabbitmq.NewConsumer(ctx, rabbitmq.ConsumerConfig{
		URL:        cfg.RabbitMQURL,
		QueueName:  "images-worker",
		RoutingKey: cloudevents.EventTypeFileUploaded,
		TracerName: "images/consumer",
	})
	if err != nil {
		return nil, err
	}

	return &Consumer{
		consumer:  consumer,
		processor: cfg.Processor,
	}, nil
}

func (c *Consumer) handleMessage(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error {
	slog.Debug("received event", "type", event.Type, "id", event.ID)
	return c.processor.Process(ctx, event.Type, event.Data)
}

func (c *Consumer) Run(ctx context.Context) error {
	return c.consumer.Run(ctx, c.handleMessage)
}

func (c *Consumer) Close() error {
	return c.consumer.Close()
}

func (c *Consumer) IsHealthy() bool {
	return c.consumer.IsHealthy()
}
