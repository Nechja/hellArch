package orchestrator

import (
	"context"
	"fmt"
	"log/slog"
	"os"

	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
)

type KubernetesOrchestrator struct {
	clientset *kubernetes.Clientset
	namespace string
	logger    *slog.Logger
}

func NewKubernetesOrchestrator() (*KubernetesOrchestrator, error) {
	var config *rest.Config
	var err error

	// Try in-cluster config first, fall back to kubeconfig
	config, err = rest.InClusterConfig()
	if err != nil {
		kubeconfig := os.Getenv("KUBECONFIG")
		if kubeconfig == "" {
			kubeconfig = os.Getenv("HOME") + "/.kube/config"
		}
		config, err = clientcmd.BuildConfigFromFlags("", kubeconfig)
		if err != nil {
			return nil, fmt.Errorf("failed to create kubernetes config: %w", err)
		}
	}

	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		return nil, fmt.Errorf("failed to create kubernetes clientset: %w", err)
	}

	namespace := os.Getenv("KUBERNETES_NAMESPACE")
	if namespace == "" {
		namespace = "hellarch"
	}

	return &KubernetesOrchestrator{
		clientset: clientset,
		namespace: namespace,
		logger:    slog.Default().With("orchestrator", "kubernetes"),
	}, nil
}

func (k *KubernetesOrchestrator) RestartService(ctx context.Context, serviceName string) error {
	labelSelector := fmt.Sprintf("app=hellarch-%s", serviceName)

	k.logger.Info("deleting pods to trigger restart", "service", serviceName, "namespace", k.namespace)

	pods, err := k.clientset.CoreV1().Pods(k.namespace).List(ctx, metav1.ListOptions{
		LabelSelector: labelSelector,
	})
	if err != nil {
		return fmt.Errorf("failed to list pods for %s: %w", serviceName, err)
	}

	if len(pods.Items) == 0 {
		k.logger.Warn("no pods found for service", "service", serviceName, "selector", labelSelector)
		return nil
	}

	for _, pod := range pods.Items {
		k.logger.Info("deleting pod", "pod", pod.Name)
		err := k.clientset.CoreV1().Pods(k.namespace).Delete(ctx, pod.Name, metav1.DeleteOptions{})
		if err != nil {
			k.logger.Error("failed to delete pod", "pod", pod.Name, "error", err)
		}
	}

	k.logger.Info("pods deleted, controller will recreate", "service", serviceName, "count", len(pods.Items))
	return nil
}

func (k *KubernetesOrchestrator) Close() error {
	return nil
}
