use chrono::{DateTime, Utc};
use serde::Deserialize;
use thiserror::Error;

#[derive(Debug, Error)]
pub enum WorkerError {
    #[error("RabbitMQ connection failed: {0}")]
    Connection(#[from] lapin::Error),

    #[error("JSON deserialization failed: {0}")]
    Deserialization(#[from] serde_json::Error),

    #[error("Handler failed: {0}")]
    Handler(String),
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum EventType {
    UserCreated,
    SessionCreated,
    AccountLinked,
}

impl EventType {
    pub fn from_str(s: &str) -> Option<Self> {
        match s {
            "hellarch.auth.user.created.v1" => Some(Self::UserCreated),
            "hellarch.auth.session.created.v1" => Some(Self::SessionCreated),
            "hellarch.auth.account.linked.v1" => Some(Self::AccountLinked),
            _ => None,
        }
    }
}

#[derive(Debug, Deserialize)]
pub struct CloudEvent<T> {
    pub specversion: String,
    pub id: String,
    pub source: String,
    #[serde(rename = "type")]
    pub event_type: String,
    pub datacontenttype: String,
    pub time: DateTime<Utc>,
    pub data: T,
}

#[derive(Debug, Deserialize)]
pub struct UserCreatedData {
    pub user_id: String,
    pub email: String,
    pub username: String,
    pub provider: String,
    pub provider_user_id: String,
    pub avatar_url: String,
    pub created_at: DateTime<Utc>,
}

#[derive(Debug, Deserialize)]
pub struct SessionCreatedData {
    pub session_id: String,
    pub user_id: String,
    pub ip_address: String,
    pub user_agent: String,
    pub created_at: DateTime<Utc>,
    pub expires_at: DateTime<Utc>,
}

#[derive(Debug, Deserialize)]
pub struct AccountLinkedData {
    pub user_id: String,
    pub provider: String,
    pub provider_user_id: String,
    pub email: String,
    pub linked_at: DateTime<Utc>,
}
