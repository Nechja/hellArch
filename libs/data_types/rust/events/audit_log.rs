use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// Audit log entry for tracking system events.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuditLog {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "event_type")]
    pub event_type: String,
    #[serde(rename = "event_id")]
    pub event_id: String,
    #[serde(rename = "source")]
    pub source: String,
    #[serde(rename = "actor_id")]
    pub actor_id: Option<String>,
    #[serde(rename = "target_id")]
    pub target_id: Option<String>,
    #[serde(rename = "metadata")]
    pub metadata: Option<String>,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
}
