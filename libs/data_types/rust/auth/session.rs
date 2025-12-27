// Example code that deserializes and serializes the model.
// extern crate serde;
// #[macro_use]
// extern crate serde_derive;
// extern crate serde_json;
//
// use generated_module::session;
//
// fn main() {
//     let json = r#"{"answer": 42}"#;
//     let model: session = serde_json::from_str(&json).unwrap();
// }

use serde::{Serialize, Deserialize};

/// User session for cookie-based auth
#[derive(Debug, Serialize, Deserialize)]
pub struct Session {
    pub created_at: String,

    pub expires_at: String,

    pub id: String,

    pub token: String,

    pub user_id: String,
}
