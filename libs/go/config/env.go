package config

import (
	"os"
	"strconv"
	"strings"
	"time"
)

func GetEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func GetEnvInt(key string, defaultValue int) int {
	if value := os.Getenv(key); value != "" {
		if i, err := strconv.Atoi(value); err == nil {
			return i
		}
	}
	return defaultValue
}

func GetEnvBool(key string, defaultValue bool) bool {
	if value := os.Getenv(key); value != "" {
		return strings.EqualFold(value, "true") || value == "1"
	}
	return defaultValue
}

func GetEnvDuration(key string, defaultValue time.Duration) time.Duration {
	if value := os.Getenv(key); value != "" {
		if d, err := time.ParseDuration(value); err == nil {
			return d
		}
	}
	return defaultValue
}

func GetEnvList(key, defaultValue string) []string {
	value := GetEnv(key, defaultValue)
	if value == "" {
		return nil
	}
	var result []string
	for _, v := range strings.Split(value, ",") {
		if trimmed := strings.TrimSpace(v); trimmed != "" {
			result = append(result, trimmed)
		}
	}
	return result
}

func RequireEnv(key string) string {
	value := os.Getenv(key)
	if value == "" {
		panic("required environment variable not set: " + key)
	}
	return value
}
