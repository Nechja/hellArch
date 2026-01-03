package orchestrator

import (
	"context"
	"fmt"
)

type Orchestrator interface {
	RestartService(ctx context.Context, serviceName string) error
	Close() error
}

func New(backend string) (Orchestrator, error) {
	switch backend {
	case "docker":
		return NewDockerOrchestrator()
	case "kubernetes":
		return NewKubernetesOrchestrator()
	default:
		return nil, fmt.Errorf("unknown orchestrator backend: %s", backend)
	}
}
