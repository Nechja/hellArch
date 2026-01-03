use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// Record of a webhook delivery attempt.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WebhookDelivery {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "webhook_id")]
    pub webhook_id: String,
    #[serde(rename = "event_id")]
    pub event_id: String,
    #[serde(rename = "event_type")]
    pub event_type: String,
    #[serde(rename = "payload")]
    pub payload: Option<String>,
    #[serde(rename = "status_code")]
    pub status_code: i32,
    #[serde(rename = "response_body")]
    pub response_body: Option<String>,
    #[serde(rename = "success")]
    pub success: bool,
    #[serde(rename = "attempt")]
    pub attempt: i32,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "webhook")]
    pub webhook: Option<Webhook>,
}
