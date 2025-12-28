use chrono::{DateTime, Utc};
use lapin::{
    options::*, types::FieldTable, Channel, Connection, ConnectionProperties, Consumer,
};
use serde::Deserialize;
use std::env;
use tokio_stream::StreamExt;
use tracing::{error, info, warn};

mod handlers;

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
    pub created_at: String,
}

#[derive(Debug, Deserialize)]
pub struct SessionCreatedData {
    pub session_id: String,
    pub user_id: String,
    pub ip_address: String,
    pub user_agent: String,
    pub created_at: String,
    pub expires_at: String,
}

#[derive(Debug, Deserialize)]
pub struct AccountLinkedData {
    pub user_id: String,
    pub provider: String,
    pub provider_user_id: String,
    pub email: String,
    pub linked_at: String,
}

const EXCHANGE_NAME: &str = "hellarch.events";
const QUEUE_NAME: &str = "events-worker";

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Initialize tracing
    tracing_subscriber::fmt()
        .json()
        .with_env_filter(
            tracing_subscriber::EnvFilter::from_default_env()
                .add_directive(tracing::Level::INFO.into()),
        )
        .init();

    let rabbitmq_url = env::var("RABBITMQ_URL")
        .unwrap_or_else(|_| "amqp://guest:guest@localhost:5672".to_string());

    info!("Connecting to RabbitMQ...");
    let conn = Connection::connect(&rabbitmq_url, ConnectionProperties::default()).await?;
    info!("Connected to RabbitMQ");

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
    consume_events(consumer).await;

    Ok(())
}

async fn setup_queue(channel: &Channel) -> Result<(), lapin::Error> {
    // Declare exchange (must match publisher)
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

    // Declare queue
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

    // Bind to all hellarch events
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
                let body = String::from_utf8_lossy(&delivery.data);

                // Extract event type from headers
                let event_type = delivery
                    .properties
                    .headers()
                    .as_ref()
                    .and_then(|h| h.inner().get("ce-type"))
                    .and_then(|v| match v {
                        lapin::types::AMQPValue::LongString(s) => Some(s.to_string()),
                        _ => None,
                    });

                match event_type.as_deref() {
                    Some("hellarch.auth.user.created.v1") => {
                        match serde_json::from_str::<CloudEvent<UserCreatedData>>(&body) {
                            Ok(event) => {
                                handlers::handle_user_created(&event.data).await;
                            }
                            Err(e) => error!(error = %e, "Failed to deserialize user.created event"),
                        }
                    }
                    Some("hellarch.auth.session.created.v1") => {
                        match serde_json::from_str::<CloudEvent<SessionCreatedData>>(&body) {
                            Ok(event) => {
                                handlers::handle_session_created(&event.data).await;
                            }
                            Err(e) => error!(error = %e, "Failed to deserialize session.created event"),
                        }
                    }
                    Some("hellarch.auth.account.linked.v1") => {
                        match serde_json::from_str::<CloudEvent<AccountLinkedData>>(&body) {
                            Ok(event) => {
                                handlers::handle_account_linked(&event.data).await;
                            }
                            Err(e) => error!(error = %e, "Failed to deserialize account.linked event"),
                        }
                    }
                    Some(t) => {
                        warn!(event_type = t, "Ignoring unknown event type");
                    }
                    None => {
                        warn!("Event missing type header");
                    }
                }

                // Acknowledge the message
                if let Err(e) = delivery.ack(BasicAckOptions::default()).await {
                    error!(error = %e, "Failed to ack message");
                }
            }
            Err(e) => {
                error!(error = %e, "Error receiving message");
            }
        }
    }
}
