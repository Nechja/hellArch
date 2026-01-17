package orchestrator

import (
	"context"
	"fmt"
	"log/slog"
)

type Orchestrator interface {
	RestartService(ctx context.Context, serviceName string) error
	Close() error
}

// Options configures orchestrator creation
type Options struct {
	Logger       *slog.Logger
	DockerClient DockerClient
	K8sPodClient KubernetesPodClient
	K8sNamespace string
}

// New creates an orchestrator for the specified backend with injected dependencies
func New(backend string, opts Options) (Orchestrator, error) {
	switch backend {
	case "docker":
		client := opts.DockerClient
		if client == nil {
			var err error
			client, err = NewDockerClient()
			if err != nil {
				return nil, fmt.Errorf("failed to create docker client: %w", err)
			}
		}
		return NewDockerOrchestrator(client, opts.Logger), nil

	case "kubernetes":
		podClient := opts.K8sPodClient
		namespace := opts.K8sNamespace
		if podClient == nil {
			clientset, ns, err := NewKubernetesClient()
			if err != nil {
				return nil, fmt.Errorf("failed to create kubernetes client: %w", err)
			}
			podClient = clientset.CoreV1().Pods(ns)
			namespace = ns
		}
		return NewKubernetesOrchestrator(podClient, namespace, opts.Logger), nil

	default:
		return nil, fmt.Errorf("unknown orchestrator backend: %s", backend)
	}
}
