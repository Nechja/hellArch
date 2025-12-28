use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

/// Extended user profile information.
Linked to auth user via AuthUserId.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserProfile {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "auth_user_id")]
    pub auth_user_id: String,
    #[serde(rename = "display_name")]
    pub display_name: Option<String>,
    #[serde(rename = "bio")]
    pub bio: Option<String>,
    #[serde(rename = "avatar_url")]
    pub avatar_url: Option<String>,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "updated_at")]
    pub updated_at: DateTime<Utc>,
    #[serde(rename = "preferences")]
    pub preferences: Option<UserPreferences>,
}
