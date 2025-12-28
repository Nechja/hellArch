use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum Theme {
    System,
    Light,
    Dark,
}

/// User preferences and settings.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserPreferences {
    #[serde(rename = "id")]
    pub id: String,
    #[serde(rename = "user_profile_id")]
    pub user_profile_id: String,
    #[serde(rename = "theme")]
    pub theme: Theme,
    #[serde(rename = "locale")]
    pub locale: Option<String>,
    #[serde(rename = "timezone")]
    pub timezone: Option<String>,
    #[serde(rename = "email_notifications")]
    pub email_notifications: bool,
    #[serde(rename = "created_at")]
    pub created_at: DateTime<Utc>,
    #[serde(rename = "updated_at")]
    pub updated_at: DateTime<Utc>,
}
