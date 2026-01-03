package cloudevents

// Auth events
const (
	EventTypeUserCreated     = "hellarch.auth.user.created.v1"
	EventTypeUserUpdated     = "hellarch.auth.user.updated.v1"
	EventTypeUserDeleted     = "hellarch.auth.user.deleted.v1"
	EventTypeSessionCreated  = "hellarch.auth.session.created.v1"
	EventTypeSessionEnded    = "hellarch.auth.session.ended.v1"
	EventTypeAccountLinked   = "hellarch.auth.account.linked.v1"
	EventTypeAccountUnlinked = "hellarch.auth.account.unlinked.v1"
)

// Security events
const (
	EventTypeLoginFailed       = "hellarch.security.login.failed.v1"
	EventTypeLoginSuspicious   = "hellarch.security.login.suspicious.v1"
	EventTypeAccountLocked     = "hellarch.security.account.locked.v1"
	EventTypeAccountUnlocked   = "hellarch.security.account.unlocked.v1"
	EventTypePasswordChanged   = "hellarch.security.password.changed.v1"
	EventTypePermissionChanged = "hellarch.security.permission.changed.v1"
	EventTypeSessionSuspicious = "hellarch.security.session.suspicious.v1"
	EventTypeTokenRevoked      = "hellarch.security.token.revoked.v1"
)

// Infrastructure events
const (
	EventTypeServiceUnhealthy      = "hellarch.infra.service.unhealthy.v1"
	EventTypeServiceRecovered      = "hellarch.infra.service.recovered.v1"
	EventTypeDatabaseConnFailed    = "hellarch.infra.database.connection.failed.v1"
	EventTypeDatabaseConnRecovered = "hellarch.infra.database.connection.recovered.v1"
	EventTypeQueueConnFailed       = "hellarch.infra.queue.connection.failed.v1"
	EventTypeQueueConnRecovered    = "hellarch.infra.queue.connection.recovered.v1"
	EventTypeCacheConnFailed       = "hellarch.infra.cache.connection.failed.v1"
	EventTypeCacheConnRecovered    = "hellarch.infra.cache.connection.recovered.v1"
	EventTypeCertificateExpiring   = "hellarch.infra.certificate.expiring.v1"
	EventTypeResourceThreshold     = "hellarch.infra.resource.threshold.v1"
)

// Operations events
const (
	EventTypeDeploymentStarted    = "hellarch.ops.deployment.started.v1"
	EventTypeDeploymentCompleted  = "hellarch.ops.deployment.completed.v1"
	EventTypeDeploymentFailed     = "hellarch.ops.deployment.failed.v1"
	EventTypeDeploymentRolledback = "hellarch.ops.deployment.rolledback.v1"
	EventTypeConfigChanged        = "hellarch.ops.config.changed.v1"
	EventTypeRateLimitExceeded    = "hellarch.ops.ratelimit.exceeded.v1"
	EventTypeWebhookFailing       = "hellarch.ops.webhook.failing.v1"
	EventTypeBackupCompleted      = "hellarch.ops.backup.completed.v1"
	EventTypeBackupFailed         = "hellarch.ops.backup.failed.v1"
)
