// Example code that deserializes and serializes the model.
// extern crate serde;
// #[macro_use]
// extern crate serde_derive;
// extern crate serde_json;
//
// use generated_module::user;
//
// fn main() {
//     let json = r#"{"answer": 42}"#;
//     let model: user = serde_json::from_str(&json).unwrap();
// }

use serde::{Serialize, Deserialize};

/// Core user identity
#[derive(Debug, Serialize, Deserialize)]
pub struct User {
    pub created_at: String,

    pub email: String,

    pub id: String,

    pub updated_at: String,

    pub username: String,
}
