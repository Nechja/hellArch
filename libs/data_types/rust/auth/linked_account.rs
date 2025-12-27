// Example code that deserializes and serializes the model.
// extern crate serde;
// #[macro_use]
// extern crate serde_derive;
// extern crate serde_json;
//
// use generated_module::linked_account;
//
// fn main() {
//     let json = r#"{"answer": 42}"#;
//     let model: linked_account = serde_json::from_str(&json).unwrap();
// }

use serde::{Serialize, Deserialize};

/// OAuth provider linked to a user
#[derive(Debug, Serialize, Deserialize)]
pub struct LinkedAccount {
    pub access_token: Option<String>,

    pub avatar_url: Option<String>,

    pub created_at: String,

    pub email: Option<String>,

    pub expires_at: Option<String>,

    pub id: String,

    pub provider: String,

    pub provider_user_id: String,

    pub refresh_token: Option<String>,

    pub updated_at: String,

    pub user_id: String,

    pub username: Option<String>,
}
