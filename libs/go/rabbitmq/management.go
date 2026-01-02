package rabbitmq

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"time"
)

// ManagementClient provides access to RabbitMQ Management API
type ManagementClient struct {
	baseURL    string
	httpClient *http.Client
}

// QueueInfo represents queue information from management API
type QueueInfo struct {
	Name      string `json:"name"`
	Consumers int    `json:"consumers"`
	Messages  int    `json:"messages"`
	State     string `json:"state"`
}

// WorkerStatus represents the health status of a worker
type WorkerStatus struct {
	Name      string `json:"name"`
	Queue     string `json:"queue"`
	Consumers int    `json:"consumers"`
	Status    string `json:"status"`
	Messages  int    `json:"messages"`
}

// NewManagementClient creates a new RabbitMQ management API client
// URL should include credentials: http://user:pass@host:15672
func NewManagementClient(managementURL string) *ManagementClient {
	return &ManagementClient{
		baseURL: managementURL,
		httpClient: &http.Client{
			Timeout: 5 * time.Second,
		},
	}
}

// GetQueueInfo retrieves information about a specific queue
func (m *ManagementClient) GetQueueInfo(ctx context.Context, queueName string) (*QueueInfo, error) {
	// URL encode the vhost (/ becomes %2F)
	endpoint := fmt.Sprintf("%s/api/queues/%s/%s", m.baseURL, url.PathEscape("/"), url.PathEscape(queueName))

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, endpoint, nil)
	if err != nil {
		return nil, err
	}

	resp, err := m.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusNotFound {
		return nil, fmt.Errorf("queue not found: %s", queueName)
	}

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	var info QueueInfo
	if err := json.NewDecoder(resp.Body).Decode(&info); err != nil {
		return nil, err
	}

	return &info, nil
}

// ListQueues retrieves all queues from RabbitMQ
func (m *ManagementClient) ListQueues(ctx context.Context) ([]QueueInfo, error) {
	endpoint := fmt.Sprintf("%s/api/queues", m.baseURL)

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, endpoint, nil)
	if err != nil {
		return nil, err
	}

	resp, err := m.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status: %d", resp.StatusCode)
	}

	var queues []QueueInfo
	if err := json.NewDecoder(resp.Body).Decode(&queues); err != nil {
		return nil, err
	}

	return queues, nil
}

// GetWorkersStatus discovers all queues with consumers (workers)
func (m *ManagementClient) GetWorkersStatus(ctx context.Context) []WorkerStatus {
	var statuses []WorkerStatus

	queues, err := m.ListQueues(ctx)
	if err != nil {
		return statuses
	}

	for _, q := range queues {
		status := WorkerStatus{
			Name:      q.Name,
			Queue:     q.Name,
			Consumers: q.Consumers,
			Messages:  q.Messages,
		}

		if q.Consumers > 0 {
			status.Status = "healthy"
		} else {
			status.Status = "unhealthy"
		}

		statuses = append(statuses, status)
	}

	return statuses
}
