use chrono::{DateTime, Utc};
use serde::Deserialize;

pub mod auth;

#[derive(Debug, Clone, Deserialize)]
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
