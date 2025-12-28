use chrono::{DateTime, Utc};
use serde::Deserialize;

/// Published when a user is deleted.
/// Event type: hellarch.auth.user.deleted.v1
#[derive(Debug, Clone, Deserialize)]
pub struct UserDeleted {
    pub user_id: String,
    pub deleted_at: DateTime<Utc>,
}
