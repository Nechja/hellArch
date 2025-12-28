use chrono::{DateTime, Utc};
use lapin::{
    options::*, types::FieldTable, Channel, Connection, ConnectionProperties, Consumer,
};
use serde::Deserialize;
use std::{env, time::Duration};
use thiserror::Error;
use tokio::signal;
use tokio_stream::StreamExt;
use tracing::{error, info, warn};

mod handlers;

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
    fn from_str(s: &str) -> Option<Self> {
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

const EXCHANGE_NAME: &str = "hellarch.events";
const QUEUE_NAME: &str = "events-worker";
const MAX_RETRIES: u32 = 10;
const INITIAL_RETRY_DELAY: Duration = Duration::from_secs(1);

#[tokio::main]
async fn main() -> Result<(), WorkerError> {
    tracing_subscriber::fmt()
        .json()
        .with_env_filter(
            tracing_subscriber::EnvFilter::from_default_env()
                .add_directive(tracing::Level::INFO.into()),
        )
        .init();

    let rabbitmq_url = env::var("RABBITMQ_URL")
        .unwrap_or_else(|_| "amqp://guest:guest@localhost:5672".to_string());

    let conn = connect_with_retry(&rabbitmq_url).await?;
    let channel = conn.create_channel().await?;
    setup_queue(&channel).await?;

    let consumer = channel
        .basic_consume(
            QUEUE_NAME,
            "events-worker-consumer",
            BasicConsumeOptions::default(),
            FieldTable::default(),
        )
        .await?;

    info!(queue = QUEUE_NAME, "Starting event consumer");

    tokio::select! {
        _ = consume_events(consumer) => {
            info!("Consumer stopped");
        }
        _ = signal::ctrl_c() => {
            info!("Received shutdown signal, gracefully stopping...");
        }
    }

    info!("Worker shutdown complete");
    Ok(())
}

async fn connect_with_retry(url: &str) -> Result<Connection, WorkerError> {
    let mut attempts = 0;
    let mut delay = INITIAL_RETRY_DELAY;

    loop {
        attempts += 1;
        info!(attempt = attempts, "Connecting to RabbitMQ...");

        match Connection::connect(url, ConnectionProperties::default()).await {
            Ok(conn) => {
                info!("Connected to RabbitMQ");
                return Ok(conn);
            }
            Err(e) if attempts < MAX_RETRIES => {
                warn!(
                    error = %e,
                    attempt = attempts,
                    retry_in_secs = delay.as_secs(),
                    "Connection failed, retrying..."
                );
                tokio::time::sleep(delay).await;
                delay = (delay * 2).min(Duration::from_secs(30));
            }
            Err(e) => {
                error!(error = %e, "Max retries exceeded, giving up");
                return Err(WorkerError::Connection(e));
            }
        }
    }
}

async fn setup_queue(channel: &Channel) -> Result<(), lapin::Error> {
    channel
        .exchange_declare(
            EXCHANGE_NAME,
            lapin::ExchangeKind::Topic,
            ExchangeDeclareOptions {
                durable: true,
                ..Default::default()
            },
            FieldTable::default(),
        )
        .await?;

    channel
        .queue_declare(
            QUEUE_NAME,
            QueueDeclareOptions {
                durable: true,
                ..Default::default()
            },
            FieldTable::default(),
        )
        .await?;

    channel
        .queue_bind(
            QUEUE_NAME,
            EXCHANGE_NAME,
            "hellarch.#",
            QueueBindOptions::default(),
            FieldTable::default(),
        )
        .await?;

    info!(exchange = EXCHANGE_NAME, queue = QUEUE_NAME, "Queue setup complete");
    Ok(())
}

async fn consume_events(mut consumer: Consumer) {
    while let Some(delivery) = consumer.next().await {
        match delivery {
            Ok(delivery) => {
                let result = process_message(&delivery.data, &delivery.properties).await;

                match result {
                    Ok(()) => {
                        if let Err(e) = delivery.ack(BasicAckOptions::default()).await {
                            error!(error = %e, "Failed to ack message");
                        }
                    }
                    Err(e) => {
                        error!(error = %e, "Handler failed, nacking message");
                        if let Err(e) = delivery
                            .nack(BasicNackOptions {
                                requeue: false, // Send to DLQ if configured
                                ..Default::default()
                            })
                            .await
                        {
                            error!(error = %e, "Failed to nack message");
                        }
                    }
                }
            }
            Err(e) => {
                error!(error = %e, "Error receiving message");
            }
        }
    }
}

async fn process_message(
    body: &[u8],
    properties: &lapin::protocol::basic::AMQPProperties,
) -> Result<(), WorkerError> {
    let body_str = String::from_utf8_lossy(body);

    let event_type_str = properties
        .headers()
        .as_ref()
        .and_then(|h| h.inner().get("ce-type"))
        .and_then(|v| match v {
            lapin::types::AMQPValue::LongString(s) => Some(s.to_string()),
            _ => None,
        });

    let Some(type_str) = event_type_str else {
        warn!("Event missing type header");
        return Ok(()); // Ack to discard malformed messages
    };

    let Some(event_type) = EventType::from_str(&type_str) else {
        warn!(event_type = %type_str, "Ignoring unknown event type");
        return Ok(()); // Ack unknown events
    };

    match event_type {
        EventType::UserCreated => {
            let event: CloudEvent<UserCreatedData> = serde_json::from_str(&body_str)?;
            handlers::handle_user_created(&event.data).await
        }
        EventType::SessionCreated => {
            let event: CloudEvent<SessionCreatedData> = serde_json::from_str(&body_str)?;
            handlers::handle_session_created(&event.data).await
        }
        EventType::AccountLinked => {
            let event: CloudEvent<AccountLinkedData> = serde_json::from_str(&body_str)?;
            handlers::handle_account_linked(&event.data).await
        }
    }
}
