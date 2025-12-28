use chrono::{DateTime, Utc};
use serde::Deserialize;

/// Published when user info is updated.
/// Event type: hellarch.auth.user.updated.v1
#[derive(Debug, Clone, Deserialize)]
pub struct UserUpdated {
    pub user_id: String,
    pub email: String,
    pub username: String,
    pub updated_at: DateTime<Utc>,
}
