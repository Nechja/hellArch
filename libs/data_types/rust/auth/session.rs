use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// User session for cookie-based authentication.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Session {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "user_id")]
    pub user_id: String,
    #[serde(rename = "token")]
    pub token: String,
    #[serde(rename = "ip_address")]
    pub ip_address: Option<String>,
    #[serde(rename = "user_agent")]
    pub user_agent: Option<String>,
    #[serde(rename = "expires_at")]
    pub expires_at: DateTime<Utc>,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
}
