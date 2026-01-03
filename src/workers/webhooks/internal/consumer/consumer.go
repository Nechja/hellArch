package consumer

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"

	cloudevents "github.com/nechja/hellarch/cloudevents"
	"github.com/nechja/hellarch/golibs/rabbitmq"
	"github.com/nechja/hellarch/workers/webhooks/internal/client"
	"github.com/nechja/hellarch/workers/webhooks/internal/dispatcher"
)

type Consumer struct {
	consumer     *rabbitmq.Consumer
	eventsClient *client.EventsClient
	dispatcher   *dispatcher.Dispatcher
}

func New(ctx context.Context, rabbitURL string, eventsClient *client.EventsClient, disp *dispatcher.Dispatcher) (*Consumer, error) {
	consumer, err := rabbitmq.NewConsumer(ctx, rabbitmq.ConsumerConfig{
		URL:        rabbitURL,
		QueueName:  "webhooks-worker",
		RoutingKey: "hellarch.#",
		TracerName: "webhooks-worker",
	})
	if err != nil {
		return nil, err
	}

	return &Consumer{
		consumer:     consumer,
		eventsClient: eventsClient,
		dispatcher:   disp,
	}, nil
}

func (c *Consumer) Close() error {
	return c.consumer.Close()
}

func (c *Consumer) IsHealthy() bool {
	return c.consumer.IsHealthy()
}

func (c *Consumer) Run(ctx context.Context) error {
	return c.consumer.Run(ctx, c.processEvent)
}

func (c *Consumer) processEvent(ctx context.Context, event cloudevents.CloudEvent[json.RawMessage]) error {
	webhooks, err := c.eventsClient.GetWebhooksForEventType(ctx, event.Type)
	if err != nil {
		return fmt.Errorf("failed to get webhooks: %w", err)
	}

	if len(webhooks) == 0 {
		slog.Debug("no webhooks registered for event type", "event_type", event.Type)
		return nil
	}

	slog.Debug("dispatching to webhooks", "event_type", event.Type, "webhook_count", len(webhooks))

	payload, err := json.Marshal(event)
	if err != nil {
		return fmt.Errorf("failed to marshal event: %w", err)
	}

	// Collect all delivery errors - if ANY fail, the message should be nacked
	var deliveryErrors []error

	for _, webhook := range webhooks {
		req := dispatcher.DeliveryRequest{
			Webhook:   webhook,
			EventID:   event.ID,
			EventType: event.Type,
			Payload:   payload,
			Attempt:   1,
		}

		if err := c.dispatcher.Deliver(ctx, req); err != nil {
			slog.Warn("webhook delivery failed",
				"webhook_id", webhook.ID,
				"event_id", event.ID,
				"error", err,
			)
			deliveryErrors = append(deliveryErrors, fmt.Errorf("webhook %s: %w", webhook.ID, err))
		}
	}

	// If any deliveries failed, return an error to nack the message
	// This sends it to the DLQ for retry/investigation
	if len(deliveryErrors) > 0 {
		return fmt.Errorf("failed to deliver to %d/%d webhooks: %w",
			len(deliveryErrors), len(webhooks), errors.Join(deliveryErrors...))
	}

	return nil
}
