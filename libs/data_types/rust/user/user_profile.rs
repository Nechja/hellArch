// Example code that deserializes and serializes the model.
// extern crate serde;
// #[macro_use]
// extern crate serde_derive;
// extern crate serde_json;
//
// use generated_module::user_profile;
//
// fn main() {
//     let json = r#"{"answer": 42}"#;
//     let model: user_profile = serde_json::from_str(&json).unwrap();
// }

use serde::{Serialize, Deserialize};

/// Extended user profile info
#[derive(Debug, Serialize, Deserialize)]
pub struct UserProfile {
    pub auth_user_id: String,

    pub avatar_url: Option<String>,

    pub bio: Option<String>,

    pub created_at: String,

    pub display_name: Option<String>,

    pub id: String,

    pub updated_at: String,
}
