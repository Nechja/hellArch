# Security

This document covers the security features implemented in HellArch.

## Authentication

### Session-Based (Browser Clients)

Browser clients authenticate via OAuth providers and receive a session cookie (`hellarch_session`). Sessions are stored in Redis with configurable expiration.

### Token-Based (Service-to-Service)

Internal services authenticate using the OAuth2 client credentials grant:

```bash
curl -X POST http://auth:5001/auth/token \
  -d "grant_type=client_credentials" \
  -d "client_id=my-service" \
  -d "client_secret=my-secret"
```

The returned Bearer token can be used with the Gateway, which validates it and sets `X-User-ID` headers for downstream services.

## JWT Security

### Token Structure

- **Algorithm**: RS256 (RSA with SHA-256)
- **JTI Claim**: Each token has a unique ID for revocation tracking
- **Expiration**: Access tokens (15 min), Refresh tokens (7 days)

### Token Revocation

Tokens can be revoked before expiration. Revoked token JTIs are stored in Redis and checked on every validation.

### Refresh Token Rotation

When a refresh token is used, the old token is immediately revoked and a new one is issued. This limits the window for token theft.

## Session Security

### Session Binding

Sessions can be bound to client IP and User-Agent to detect session hijacking:

| Mode | Behavior |
|------|----------|
| `none` | No binding validation |
| `warn` | Log mismatches but allow |
| `strict` | Reject mismatches with 401 |

Configure via `SESSION_BINDING_MODE` environment variable.

### Max Sessions

Users are limited to a configurable number of concurrent sessions. When exceeded, the oldest sessions are automatically revoked.

## SSRF Protection

### Webhook URL Validation

When registering webhooks, URLs are validated to prevent SSRF attacks:

1. **Scheme validation** - Only `http` and `https` allowed
2. **DNS resolution** - Hostname must resolve (prevents DNS rebinding setup)
3. **Private IP blocking** - Blocks localhost, private ranges (10.x, 172.16-31.x, 192.168.x), link-local (169.254.x)

### Delivery-Time Protection

The webhook worker uses an SSRF-safe HTTP client that validates resolved IPs at connection time, preventing DNS rebinding attacks where a hostname resolves to a public IP during registration but a private IP during delivery.

**Blocked ranges:**
- `127.0.0.0/8` - Loopback
- `10.0.0.0/8` - Private
- `172.16.0.0/12` - Private
- `192.168.0.0/16` - Private
- `169.254.0.0/16` - Link-local / Cloud metadata
- `fc00::/7` - IPv6 unique local

## Data Protection

### Sensitive Field Redaction

Audit logs and dead letter storage automatically redact sensitive fields to prevent credential leakage:

**Redacted field patterns:**
- `password`, `secret`, `token`, `api_key`
- `access_token`, `refresh_token`, `session_token`
- `credential`, `authorization`, `private_key`
- `credit_card`, `card_number`, `cvv`, `ssn`
- And more...

Fields containing these patterns (case-insensitive) are replaced with `[REDACTED]`.

### File Upload Security

- **Content-Type validation** - Detected content type must match claimed type
- **Executable blocking** - Blocks executables disguised as images
- **Filename sanitization** - Prevents path traversal and header injection
- **Size limits** - Configurable maximum upload size

## Gateway Security

### Rate Limiting

Per-IP rate limiting with configurable limits:

| Endpoint Type | Default Limit |
|--------------|---------------|
| General | 100 req/min |
| Auth endpoints | 10 req/min |

### Permission-Based Access

Routes can require specific permissions:

```json
{
  "PermissionRoutes": {
    "/admin": "admin.panel",
    "/api/events": "admin.panel"
  }
}
```

### Security Headers

All responses include security headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`

## Cryptographic Operations

### Constant-Time Comparison

All credential comparisons use constant-time algorithms to prevent timing attacks:
- Service account secrets
- CSRF tokens
- Session tokens

## Environment Configuration

### Required Secrets

| Variable | Description |
|----------|-------------|
| `JWT_PRIVATE_KEY_PATH` | Path to RSA private key for signing |
| `JWT_PUBLIC_KEY_PATH` | Path to RSA public key for verification |
| `SERVICE_ACCOUNTS` | Service credentials (production) |

### Security-Related Settings

| Variable | Default | Description |
|----------|---------|-------------|
| `SESSION_BINDING_MODE` | `warn` | Session binding strictness |
| `MAX_SESSIONS_PER_USER` | `5` | Concurrent session limit |
| `MAX_UPLOAD_SIZE` | `1GB` | File upload size limit |
