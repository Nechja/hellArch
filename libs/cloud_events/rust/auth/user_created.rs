use chrono::{DateTime, Utc};
use serde::Deserialize;

/// Published when a new user is created via OAuth.
/// Event type: hellarch.auth.user.created.v1
#[derive(Debug, Clone, Deserialize)]
pub struct UserCreated {
    pub user_id: String,
    pub email: String,
    pub username: String,
    pub provider: String,
    pub provider_user_id: String,
    pub avatar_url: String,
    pub created_at: DateTime<Utc>,
}
