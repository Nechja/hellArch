use chrono::{DateTime, Utc};
use serde::Deserialize;

/// Published when an OAuth account is linked to a user.
/// Event type: hellarch.auth.account.linked.v1
#[derive(Debug, Clone, Deserialize)]
pub struct AccountLinked {
    pub user_id: String,
    pub provider: String,
    pub provider_user_id: String,
    pub email: String,
    pub linked_at: DateTime<Utc>,
}
