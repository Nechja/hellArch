use serde::{Deserialize, Serialize};

/// Centralized permission constants used across all services.
/// These are the keys stored in the user's permissions JSONB field.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Permissions {
}
