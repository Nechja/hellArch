use chrono::{DateTime, Utc};
use serde::Deserialize;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum SessionEndReason {
    Logout,
    Expired,
    Revoked,
}

/// Published when a new session is created.
/// Event type: hellarch.auth.session.created.v1
#[derive(Debug, Clone, Deserialize)]
pub struct SessionCreated {
    pub session_id: String,
    pub user_id: String,
    pub ip_address: String,
    pub user_agent: String,
    pub created_at: DateTime<Utc>,
    pub expires_at: DateTime<Utc>,
}
