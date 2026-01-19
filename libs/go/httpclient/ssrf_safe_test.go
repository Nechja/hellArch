package httpclient

import (
	"net"
	"testing"
)

func TestIsPrivateIPBlocksLoopback(t *testing.T) {
	loopbackIPs := []string{
		"127.0.0.1",
		"127.0.0.255",
		"127.255.255.255",
		"::1",
	}

	for _, ipStr := range loopbackIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should block loopback IP: %s", ipStr)
		}
	}
}

func TestIsPrivateIPBlocksPrivateRanges(t *testing.T) {
	privateIPs := []string{
		// 10.0.0.0/8
		"10.0.0.0",
		"10.0.0.1",
		"10.255.255.255",
		// 172.16.0.0/12
		"172.16.0.0",
		"172.16.0.1",
		"172.31.255.255",
		// 192.168.0.0/16
		"192.168.0.0",
		"192.168.0.1",
		"192.168.255.255",
	}

	for _, ipStr := range privateIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should block private IP: %s", ipStr)
		}
	}
}

func TestIsPrivateIPBlocksLinkLocal(t *testing.T) {
	linkLocalIPs := []string{
		"169.254.0.1",
		"169.254.169.254", // AWS/GCP/Azure metadata endpoint
		"169.254.255.255",
	}

	for _, ipStr := range linkLocalIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should block link-local IP (cloud metadata): %s", ipStr)
		}
	}
}

func TestIsPrivateIPBlocksUnspecified(t *testing.T) {
	unspecifiedIPs := []string{
		"0.0.0.0",
		"::",
	}

	for _, ipStr := range unspecifiedIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should block unspecified IP: %s", ipStr)
		}
	}
}

func TestIsPrivateIPAllowsPublicIPs(t *testing.T) {
	publicIPs := []string{
		"8.8.8.8",        // Google DNS
		"1.1.1.1",        // Cloudflare DNS
		"93.184.216.34",  // example.com
		"172.15.255.255", // Just below 172.16.0.0/12
		"172.32.0.0",     // Just above 172.16.0.0/12
		"192.167.255.255", // Just below 192.168.0.0/16
		"192.169.0.0",    // Just above 192.168.0.0/16
		"11.0.0.1",       // Just above 10.0.0.0/8
	}

	for _, ipStr := range publicIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if isPrivateIP(ip) {
			t.Errorf("should allow public IP: %s", ipStr)
		}
	}
}

func TestIsPrivateIPBlocksIPv6UniqueLocal(t *testing.T) {
	// fc00::/7 - unique local addresses (IPv6 equivalent of private)
	uniqueLocalIPs := []string{
		"fc00::1",
		"fd00::1",
		"fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
	}

	for _, ipStr := range uniqueLocalIPs {
		ip := net.ParseIP(ipStr)
		if ip == nil {
			t.Fatalf("failed to parse IP: %s", ipStr)
		}
		if !isPrivateIP(ip) {
			t.Errorf("should block IPv6 unique local address: %s", ipStr)
		}
	}
}

func TestNewSSRFSafeReturnsClient(t *testing.T) {
	client := NewSSRFSafe(10)
	if client == nil {
		t.Error("NewSSRFSafe should return a non-nil client")
	}
	if client.Transport == nil {
		t.Error("client should have a transport configured")
	}
}
