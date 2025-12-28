use lapin::{options::*, types::FieldTable, Channel, Connection, ConnectionProperties};
use std::time::Duration;
use tracing::{error, info, warn};

use crate::types::WorkerError;

pub const EXCHANGE_NAME: &str = "hellarch.events";
pub const QUEUE_NAME: &str = "events-worker";

const MAX_RETRIES: u32 = 10;
const INITIAL_RETRY_DELAY: Duration = Duration::from_secs(1);

pub async fn connect_with_retry(url: &str) -> Result<Connection, WorkerError> {
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

pub async fn setup_queue(channel: &Channel) -> Result<(), lapin::Error> {
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
