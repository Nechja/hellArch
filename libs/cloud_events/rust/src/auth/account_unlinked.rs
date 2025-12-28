use chrono::{DateTime, Utc};
use serde::Deserialize;

/// Published when an OAuth account is unlinked from a user.
/// Event type: hellarch.auth.account.unlinked.v1
#[derive(Debug, Clone, Deserialize)]
pub struct AccountUnlinked {
    pub user_id: String,
    pub provider: String,
    pub unlinked_at: DateTime<Utc>,
}
