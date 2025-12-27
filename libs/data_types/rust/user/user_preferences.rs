// Example code that deserializes and serializes the model.
// extern crate serde;
// #[macro_use]
// extern crate serde_derive;
// extern crate serde_json;
//
// use generated_module::user_preferences;
//
// fn main() {
//     let json = r#"{"answer": 42}"#;
//     let model: user_preferences = serde_json::from_str(&json).unwrap();
// }

use serde::{Serialize, Deserialize};

/// User preferences and settings
#[derive(Debug, Serialize, Deserialize)]
pub struct UserPreferences {
    pub created_at: String,

    pub email_notifications: bool,

    pub id: String,

    pub locale: Option<String>,

    pub theme: Theme,

    pub timezone: Option<String>,

    pub updated_at: String,

    pub user_profile_id: String,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum Theme {
    Dark,

    Light,

    System,
}
