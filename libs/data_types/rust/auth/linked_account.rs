use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// OAuth provider account linked to a user.
/// Stores provider-specific identity info. Token storage is optional
/// (only needed if we want to call provider APIs on behalf of user).
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LinkedAccount {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "user_id")]
    pub user_id: String,
    #[serde(rename = "provider")]
    pub provider: String,
    #[serde(rename = "provider_user_id")]
    pub provider_user_id: String,
    #[serde(rename = "email")]
    pub email: Option<String>,
    #[serde(rename = "username")]
    pub username: Option<String>,
    #[serde(rename = "avatar_url")]
    pub avatar_url: Option<String>,
    #[serde(rename = "access_token")]
    pub access_token: Option<String>,
    #[serde(rename = "refresh_token")]
    pub refresh_token: Option<String>,
    #[serde(rename = "token_expires_at")]
    pub token_expires_at: Option<DateTime<Utc>>,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "updated_at")]
    pub updated_at: DateTime<Utc>,
    #[serde(rename = "user")]
    pub user: Option<User>,
}
