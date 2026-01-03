use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum WebhookFormat {
    Raw,
    Discord,
    Slack,
}

/// Webhook endpoint registration for event delivery.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Webhook {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "name")]
    pub name: String,
    #[serde(rename = "url")]
    pub url: String,
    #[serde(rename = "secret")]
    pub secret: String,
    #[serde(rename = "event_types")]
    pub event_types: Vec<String>,
    #[serde(rename = "format")]
    pub format: WebhookFormat,
    #[serde(rename = "is_active")]
    pub is_active: bool,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "updated_at")]
    pub updated_at: DateTime<Utc>,
    #[serde(rename = "deliveries")]
    pub deliveries: Vec<WebhookDelivery>,
}
