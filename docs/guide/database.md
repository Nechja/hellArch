# Database Setup

EF Core manages schema definitions. Migrations run automatically when DB containers start.

## Architecture

```mermaid
graph TB
    subgraph Build
        SCHEMA[Schema Project] --> RUNNER[MigrationRunner]
        RUNNER --> BINARY[migrate binary]
    end

    subgraph Container
        BINARY --> ENTRYPOINT[entrypoint.sh]
        ENTRYPOINT --> PG_START[Start Postgres]
        PG_START --> PG_READY{pg_isready?}
        PG_READY -->|yes| MIGRATE[Run Migrations]
        MIGRATE --> RUNNING[Container Ready]
    end
```

## DbContexts

Both contexts use .NET 10 primary constructors and snake_case naming.

### AuthDbContext

```csharp
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<LinkedAccount> LinkedAccounts => Set<LinkedAccount>();
}
```

**Tables:**
| Table | Primary Key | Indexes |
|-------|-------------|---------|
| `users` | `id` (uuid) | `email` (unique) |
| `sessions` | `id` (uuid) | `token` (unique), `user_id` |
| `linked_accounts` | `id` (uuid) | `(provider, provider_user_id)` (unique), `user_id` |

**Users Columns:**
| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `id` | uuid | `gen_random_uuid()` | Primary key |
| `email` | text | - | User email (unique) |
| `username` | text | - | Display name |
| `is_active` | boolean | `true` | Account status |
| `permissions` | jsonb | `'{}'::jsonb` | Permission map |
| `created_at` | timestamp | `now()` | Creation time |
| `updated_at` | timestamp | `now()` | Last update |

### UserDbContext

```csharp
public sealed class UserDbContext(DbContextOptions<UserDbContext> options)
    : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
}
```

**Tables:**
| Table | Primary Key | Indexes |
|-------|-------------|---------|
| `user_profiles` | `id` (uuid) | `auth_user_id` (unique) |
| `user_preferences` | `id` (uuid) | `user_profile_id` (FK) |

## Migration Runner

Standalone .NET console app that runs migrations on container startup.

### Usage

```bash
migrate <context> <connection-string>
```

- `context`: `auth` or `user`
- `connection-string`: Npgsql connection string

### How It Works

```mermaid
sequenceDiagram
    participant E as entrypoint.sh
    participant P as PostgreSQL
    participant M as migrate
    participant J as Jaeger

    E->>P: Start in background
    E->>E: Wait for pg_isready
    P-->>E: Ready
    E->>M: migrate auth $CONNECTION_STRING
    M->>P: Apply pending migrations
    M->>J: Export trace spans
    M-->>E: Success (exit 0)
    E->>E: wait $POSTGRES_PID
```

### Failure Handling

If migrations fail, the container stops immediately to prevent running with an inconsistent schema:

```mermaid
graph LR
    MIGRATE[Run Migrations] -->|success| WAIT[Keep Running]
    MIGRATE -->|failure| KILL[Kill Postgres]
    KILL --> EXIT[Exit 1]
```

### OpenTelemetry Tracing

The migration runner exports traces to Jaeger when `OTEL_EXPORTER_OTLP_ENDPOINT` is set:

- `database.migrate` - Root span for entire migration run
- `migrations.apply` - Span covering all pending migrations
- `migration.step` - Individual migration span with name tag

View migration traces at [http://localhost:16686](http://localhost:16686)

### Container Build

Two-stage Dockerfile:

```dockerfile
# Stage 1: Build migration runner
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
RUN dotnet publish MigrationRunner.csproj -r linux-x64 -o /app/publish

# Stage 2: Postgres + migrate binary
FROM postgres:17-bookworm
COPY --from=build /app/publish/MigrationRunner /usr/local/bin/migrate
COPY entrypoint.sh /usr/local/bin/entrypoint-wrapper.sh
ENTRYPOINT ["/usr/local/bin/entrypoint-wrapper.sh"]
```

## Database Defaults

PostgreSQL functions for defaults:

| Column | Default |
|--------|---------|
| `id` | `gen_random_uuid()` |
| `created_at` | `now()` |
| `updated_at` | `now()` |

## Relationships

```mermaid
erDiagram
    users ||--o{ sessions : "has many"
    users ||--o{ linked_accounts : "has many"
    user_profiles ||--o| user_preferences : "has one"

    sessions {
        uuid user_id FK
    }
    linked_accounts {
        uuid user_id FK
    }
    user_preferences {
        uuid user_profile_id FK
    }
```

All foreign keys use `CASCADE` delete.

## Creating Migrations

```bash
cd src/schema/Schema

# Auth context
dotnet ef migrations add MigrationName \
    --context AuthDbContext \
    --output-dir Migrations/Auth

# User context
dotnet ef migrations add MigrationName \
    --context UserDbContext \
    --output-dir Migrations/User
```

After creating migrations, rebuild DB images to include them:

```bash
tilt up
# or
docker compose -f infra/docker/docker-compose.infra.yml build
```

## Project Structure

```
src/schema/
├── Schema/
│   ├── Contexts/
│   │   ├── AuthDbContext.cs
│   │   └── UserDbContext.cs
│   ├── Entities/
│   │   ├── Auth/
│   │   └── User/
│   └── Migrations/
│       ├── Auth/
│       └── User/
└── MigrationRunner/
    └── Program.cs

infra/dbs/
├── auth-db/
│   ├── Dockerfile
│   └── entrypoint.sh
└── user-db/
    ├── Dockerfile
    └── entrypoint.sh
```

## Conventions

| Convention | Implementation |
|------------|----------------|
| Naming | `UseSnakeCaseNamingConvention()` |
| Primary Keys | UUID with `gen_random_uuid()` |
| Timestamps | `now()` default |
| Enums | Stored as strings |
| Cascade | All FK relationships |
