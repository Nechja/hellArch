use lapin::{options::*, types::FieldTable, Consumer};
use std::env;
use tokio::signal;
use tokio_stream::StreamExt;
use tracing::{error, info, warn};

mod handlers;
mod rabbitmq;
mod types;

pub use types::*;

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

    let conn = rabbitmq::connect_with_retry(&rabbitmq_url).await?;
    let channel = conn.create_channel().await?;
    rabbitmq::setup_queue(&channel).await?;

    let consumer = channel
        .basic_consume(
            rabbitmq::QUEUE_NAME,
            "events-worker-consumer",
            BasicConsumeOptions::default(),
            FieldTable::default(),
        )
        .await?;

    info!(queue = rabbitmq::QUEUE_NAME, "Starting event consumer");

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
                                requeue: false,
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
        return Ok(());
    };

    let Some(event_type) = EventType::from_str(&type_str) else {
        warn!(event_type = %type_str, "Ignoring unknown event type");
        return Ok(());
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
