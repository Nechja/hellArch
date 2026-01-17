package orchestrator

import (
	"context"
	"fmt"
	"io"
	"log/slog"

	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/client"
)

// DockerClient defines the Docker operations we need (for testability)
type DockerClient interface {
	ContainerRestart(ctx context.Context, containerID string, options container.StopOptions) error
	Close() error
}

type DockerOrchestrator struct {
	client DockerClient
	logger *slog.Logger
}

// NewDockerClient creates a real Docker client from environment
func NewDockerClient() (DockerClient, error) {
	return client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
}

func NewDockerOrchestrator(dockerClient DockerClient, logger *slog.Logger) *DockerOrchestrator {
	if logger == nil {
		logger = slog.New(slog.NewTextHandler(io.Discard, nil))
	}
	return &DockerOrchestrator{
		client: dockerClient,
		logger: logger.With("orchestrator", "docker"),
	}
}

func (d *DockerOrchestrator) RestartService(ctx context.Context, serviceName string) error {
	containerName := "hellarch-" + serviceName

	d.logger.Info("restarting container", "container", containerName)

	timeout := 10
	err := d.client.ContainerRestart(ctx, containerName, container.StopOptions{
		Timeout: &timeout,
	})
	if err != nil {
		return fmt.Errorf("failed to restart container %s: %w", containerName, err)
	}

	d.logger.Info("container restarted successfully", "container", containerName)
	return nil
}

func (d *DockerOrchestrator) Close() error {
	return d.client.Close()
}
