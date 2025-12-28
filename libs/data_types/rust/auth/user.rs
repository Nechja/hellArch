use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// Core user identity in the auth system.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct User {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "email")]
    pub email: String,
    #[serde(rename = "username")]
    pub username: String,
    #[serde(rename = "is_active")]
    pub is_active: bool,
    #[serde(rename = "permissions")]
    pub permissions: Option<Dictionary<string, bool>>,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "updated_at")]
    pub updated_at: DateTime<Utc>,
    #[serde(rename = "sessions")]
    pub sessions: Vec<Session>,
    #[serde(rename = "linked_accounts")]
    pub linked_accounts: Vec<LinkedAccount>,
}
