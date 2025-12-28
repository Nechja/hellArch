use thiserror::Error;

pub use hellarch_cloud_events::CloudEvent;
pub use hellarch_cloud_events::auth::{
    AccountLinked, SessionCreated, UserCreated,
};

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
