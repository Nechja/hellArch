package dispatcher

import (
	"encoding/json"
	"fmt"
	"strings"
	"time"

	"github.com/nechja/hellarch/workers/webhooks/internal/client"
)

// CloudEvent represents the incoming CloudEvents format
type CloudEvent struct {
	SpecVersion     string          `json:"specversion"`
	Type            string          `json:"type"`
	Source          string          `json:"source"`
	Subject         string          `json:"subject,omitempty"`
	ID              string          `json:"id"`
	Time            string          `json:"time,omitempty"`
	DataContentType string          `json:"datacontenttype,omitempty"`
	Data            json.RawMessage `json:"data,omitempty"`
}

// DiscordPayload represents Discord webhook format
type DiscordPayload struct {
	Content string         `json:"content,omitempty"`
	Embeds  []DiscordEmbed `json:"embeds,omitempty"`
}

type DiscordEmbed struct {
	Title       string              `json:"title,omitempty"`
	Description string              `json:"description,omitempty"`
	Color       int                 `json:"color,omitempty"`
	Fields      []DiscordEmbedField `json:"fields,omitempty"`
	Footer      *DiscordEmbedFooter `json:"footer,omitempty"`
	Timestamp   string              `json:"timestamp,omitempty"`
}

type DiscordEmbedField struct {
	Name   string `json:"name"`
	Value  string `json:"value"`
	Inline bool   `json:"inline,omitempty"`
}

type DiscordEmbedFooter struct {
	Text string `json:"text"`
}

// SlackPayload represents Slack webhook format
type SlackPayload struct {
	Text   string       `json:"text,omitempty"`
	Blocks []SlackBlock `json:"blocks,omitempty"`
}

type SlackBlock struct {
	Type     string         `json:"type"`
	Text     *SlackText     `json:"text,omitempty"`
	Elements []SlackElement `json:"elements,omitempty"`
	Fields   []SlackText    `json:"fields,omitempty"`
}

type SlackText struct {
	Type string `json:"type"`
	Text string `json:"text"`
}

type SlackElement struct {
	Type string `json:"type"`
	Text string `json:"text"`
}

// TransformPayload converts CloudEvents payload to the specified format
func TransformPayload(payload []byte, format client.WebhookFormat) ([]byte, string, error) {
	switch format {
	case client.WebhookFormatDiscord:
		return transformToDiscord(payload)
	case client.WebhookFormatSlack:
		return transformToSlack(payload)
	default:
		return payload, "application/cloudevents+json", nil
	}
}

func transformToDiscord(payload []byte) ([]byte, string, error) {
	var event CloudEvent
	if err := json.Unmarshal(payload, &event); err != nil {
		return nil, "", fmt.Errorf("failed to parse CloudEvent: %w", err)
	}

	color := getColorForEventType(event.Type)
	title := formatEventTitle(event.Type)

	embed := DiscordEmbed{
		Title:     title,
		Color:     color,
		Timestamp: event.Time,
		Footer: &DiscordEmbedFooter{
			Text: fmt.Sprintf("Source: %s", event.Source),
		},
	}

	// Add fields from event data
	fields := extractFields(event.Data)
	if len(fields) > 0 {
		embed.Fields = fields
	}

	// Add subject if present
	if event.Subject != "" {
		embed.Description = fmt.Sprintf("Subject: `%s`", event.Subject)
	}

	discordPayload := DiscordPayload{
		Embeds: []DiscordEmbed{embed},
	}

	result, err := json.Marshal(discordPayload)
	if err != nil {
		return nil, "", fmt.Errorf("failed to marshal Discord payload: %w", err)
	}

	return result, "application/json", nil
}

func transformToSlack(payload []byte) ([]byte, string, error) {
	var event CloudEvent
	if err := json.Unmarshal(payload, &event); err != nil {
		return nil, "", fmt.Errorf("failed to parse CloudEvent: %w", err)
	}

	title := formatEventTitle(event.Type)

	blocks := []SlackBlock{
		{
			Type: "header",
			Text: &SlackText{
				Type: "plain_text",
				Text: title,
			},
		},
	}

	// Add event info section
	infoText := fmt.Sprintf("*Source:* %s\n*Event ID:* %s", event.Source, event.ID)
	if event.Subject != "" {
		infoText += fmt.Sprintf("\n*Subject:* %s", event.Subject)
	}

	blocks = append(blocks, SlackBlock{
		Type: "section",
		Text: &SlackText{
			Type: "mrkdwn",
			Text: infoText,
		},
	})

	// Add data fields
	fields := extractSlackFields(event.Data)
	if len(fields) > 0 {
		blocks = append(blocks, SlackBlock{
			Type:   "section",
			Fields: fields,
		})
	}

	// Add timestamp context
	if event.Time != "" {
		blocks = append(blocks, SlackBlock{
			Type: "context",
			Elements: []SlackElement{
				{
					Type: "mrkdwn",
					Text: fmt.Sprintf("Received at %s", event.Time),
				},
			},
		})
	}

	slackPayload := SlackPayload{
		Text:   title,
		Blocks: blocks,
	}

	result, err := json.Marshal(slackPayload)
	if err != nil {
		return nil, "", fmt.Errorf("failed to marshal Slack payload: %w", err)
	}

	return result, "application/json", nil
}

func formatEventTitle(eventType string) string {
	// Convert hellarch.auth.user.created.v1 to "User Created"
	parts := strings.Split(eventType, ".")
	if len(parts) >= 4 {
		// Get the action parts (e.g., "user", "created")
		action := parts[len(parts)-3] + " " + parts[len(parts)-2]
		return strings.Title(action)
	}
	return eventType
}

func getColorForEventType(eventType string) int {
	switch {
	case strings.Contains(eventType, "created"):
		return 0x2ECC71 // Green
	case strings.Contains(eventType, "deleted"):
		return 0xE74C3C // Red
	case strings.Contains(eventType, "updated"):
		return 0x3498DB // Blue
	case strings.Contains(eventType, "failed"):
		return 0xE74C3C // Red
	case strings.Contains(eventType, "unhealthy"):
		return 0xE74C3C // Red
	case strings.Contains(eventType, "recovered"):
		return 0x2ECC71 // Green
	default:
		return 0x9B59B6 // Purple
	}
}

func extractFields(data json.RawMessage) []DiscordEmbedField {
	if len(data) == 0 {
		return nil
	}

	var dataMap map[string]any
	if err := json.Unmarshal(data, &dataMap); err != nil {
		return nil
	}

	var fields []DiscordEmbedField
	for key, value := range dataMap {
		valueStr := formatValue(value)
		if valueStr != "" && len(fields) < 25 { // Discord limit
			fields = append(fields, DiscordEmbedField{
				Name:   formatFieldName(key),
				Value:  truncate(valueStr, 1024),
				Inline: len(valueStr) < 50,
			})
		}
	}

	return fields
}

func extractSlackFields(data json.RawMessage) []SlackText {
	if len(data) == 0 {
		return nil
	}

	var dataMap map[string]any
	if err := json.Unmarshal(data, &dataMap); err != nil {
		return nil
	}

	var fields []SlackText
	for key, value := range dataMap {
		valueStr := formatValue(value)
		if valueStr != "" && len(fields) < 10 { // Slack limit
			fields = append(fields, SlackText{
				Type: "mrkdwn",
				Text: fmt.Sprintf("*%s:*\n%s", formatFieldName(key), truncate(valueStr, 200)),
			})
		}
	}

	return fields
}

func formatFieldName(name string) string {
	// Convert snake_case to Title Case
	name = strings.ReplaceAll(name, "_", " ")
	return strings.Title(name)
}

func formatValue(value any) string {
	switch v := value.(type) {
	case string:
		return v
	case float64:
		if v == float64(int64(v)) {
			return fmt.Sprintf("%d", int64(v))
		}
		return fmt.Sprintf("%.2f", v)
	case bool:
		return fmt.Sprintf("%t", v)
	case nil:
		return ""
	case map[string]any, []any:
		b, _ := json.Marshal(v)
		return string(b)
	default:
		return fmt.Sprintf("%v", v)
	}
}

func truncate(s string, maxLen int) string {
	if len(s) <= maxLen {
		return s
	}
	return s[:maxLen-3] + "..."
}

// GetCurrentTimestamp returns current time in RFC3339 format
func GetCurrentTimestamp() string {
	return time.Now().UTC().Format(time.RFC3339)
}
