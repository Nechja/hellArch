use chrono::{DateTime, Utc};
use serde::Deserialize;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum SessionEndReason {
    Logout,
    Expired,
    Revoked,
}

/// Published when a session ends (logout or expiry).
/// Event type: hellarch.auth.session.ended.v1
#[derive(Debug, Clone, Deserialize)]
pub struct SessionEnded {
    pub session_id: String,
    pub user_id: String,
    pub reason: SessionEndReason,
    pub ended_at: DateTime<Utc>,
}
