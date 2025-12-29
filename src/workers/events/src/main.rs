use lapin::{options::*, types::FieldTable, Consumer};
use opentelemetry::{trace::TracerProvider, KeyValue};
use opentelemetry_otlp::WithExportConfig;
use opentelemetry_sdk::{runtime, trace::TracerProvider as SdkTracerProvider, Resource};
use std::env;
use tokio::signal;
use tokio_stream::StreamExt;
use tracing::{error, info, info_span, warn, Instrument};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

mod handlers;
mod rabbitmq;
mod types;

pub use types::*;

fn init_telemetry() -> SdkTracerProvider {
    let otel_endpoint =
        env::var("OTEL_EXPORTER_OTLP_ENDPOINT").unwrap_or_else(|_| "http://localhost:4317".into());

    let service_name = env::var("OTEL_SERVICE_NAME").unwrap_or_else(|_| "events-worker".into());

    let exporter = opentelemetry_otlp::SpanExporter::builder()
        .with_tonic()
        .with_endpoint(&otel_endpoint)
        .build()
        .expect("Failed to create OTLP exporter");

    let resource = Resource::new(vec![KeyValue::new("service.name", service_name)]);

    let provider = SdkTracerProvider::builder()
        .with_batch_exporter(exporter, runtime::Tokio)
        .with_resource(resource)
        .build();

    opentelemetry::global::set_tracer_provider(provider.clone());

    provider
}

#[tokio::main]
async fn main() -> Result<(), WorkerError> {
    let tracer_provider = init_telemetry();
    let tracer = tracer_provider.tracer("events-worker");
    let otel_layer = tracing_opentelemetry::layer().with_tracer(tracer);

    tracing_subscriber::registry()
        .with(
            tracing_subscriber::EnvFilter::from_default_env()
                .add_directive(tracing::Level::INFO.into()),
        )
        .with(tracing_subscriber::fmt::layer().json())
        .with(otel_layer)
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

    info!("Flushing telemetry...");
    if let Err(e) = tracer_provider.shutdown() {
        error!(error = %e, "Failed to shutdown tracer provider");
    }

    info!("Worker shutdown complete");
    Ok(())
}

async fn consume_events(mut consumer: Consumer) {
    while let Some(delivery) = consumer.next().await {
        match delivery {
            Ok(delivery) => {
                let event_type = extract_event_type(&delivery.properties);
                let span = info_span!(
                    "process_event",
                    otel.kind = "consumer",
                    messaging.system = "rabbitmq",
                    messaging.operation = "receive",
                    event.type = event_type.as_deref().unwrap_or("unknown"),
                );

                let result = process_message(&delivery.data, &delivery.properties)
                    .instrument(span)
                    .await;

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

fn extract_event_type(properties: &lapin::protocol::basic::AMQPProperties) -> Option<String> {
    properties
        .headers()
        .as_ref()
        .and_then(|h| h.inner().get("ce-type"))
        .and_then(|v| match v {
            lapin::types::AMQPValue::LongString(s) => Some(s.to_string()),
            _ => None,
        })
}

async fn process_message(
    body: &[u8],
    properties: &lapin::protocol::basic::AMQPProperties,
) -> Result<(), WorkerError> {
    let body_str = String::from_utf8_lossy(body);

    let Some(type_str) = extract_event_type(properties) else {
        warn!("Event missing type header");
        return Ok(());
    };

    let Some(event_type) = EventType::from_str(&type_str) else {
        warn!(event_type = %type_str, "Ignoring unknown event type");
        return Ok(());
    };

    match event_type {
        EventType::UserCreated => {
            let event: CloudEvent<UserCreated> = serde_json::from_str(&body_str)?;
            handlers::handle_user_created(&event.data).await
        }
        EventType::SessionCreated => {
            let event: CloudEvent<SessionCreated> = serde_json::from_str(&body_str)?;
            handlers::handle_session_created(&event.data).await
        }
        EventType::AccountLinked => {
            let event: CloudEvent<AccountLinked> = serde_json::from_str(&body_str)?;
            handlers::handle_account_linked(&event.data).await
        }
    }
}
