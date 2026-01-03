package orchestrator

import (
	"context"
	"fmt"
	"log/slog"

	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/client"
)

type DockerOrchestrator struct {
	client *client.Client
	logger *slog.Logger
}

func NewDockerOrchestrator() (*DockerOrchestrator, error) {
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return nil, fmt.Errorf("failed to create docker client: %w", err)
	}

	return &DockerOrchestrator{
		client: cli,
		logger: slog.Default().With("orchestrator", "docker"),
	}, nil
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
