# Testing

**Python + pytest**

API and integration tests using pytest with async support.

## Architecture

```mermaid
graph LR
    subgraph Test Suite
        AUTH_T[test_auth.py]
        ADMIN_T[test_admin.py]
        USER_T[test_user.py]
    end

    subgraph Fixtures
        DB[Database<br/>psycopg]
        HTTP[HTTP Client<br/>httpx]
        JWT[JWT Tokens<br/>python-jose]
        MOCK[OAuth Mocks<br/>respx]
    end

    subgraph Services
        AUTH[Auth :5001]
        USER[User :5002]
        EVENTS[Events :5005]
        FILES[Files :5006]
        IDENTITY[Identity :5010]
    end

    AUTH_T --> HTTP
    AUTH_T --> JWT
    AUTH_T --> DB
    ADMIN_T --> HTTP
    ADMIN_T --> DB
    USER_T --> HTTP
    HTTP --> AUTH
    HTTP --> USER
```

## Stack

| Tool | Purpose |
|------|---------|
| pytest | Test framework |
| pytest-asyncio | Async test support (auto mode) |
| pytest-xdist | Parallel execution |
| pytest-cov | Coverage reporting |
| pytest-timeout | Test timeout enforcement |
| httpx | Async HTTP client |
| respx | HTTP mocking for OAuth providers |
| psycopg | PostgreSQL async access |
| python-jose | JWT token creation/validation |
| pydantic | Data validation |
| faker | Fake data generation |
| redis | Redis client for cache tests |
| aio-pika | RabbitMQ client for event tests |
| hellarch-types | Generated Python types |

## Setup

```bash
cd tests
uv sync
```

Uses the generated Python types from `libs/data_types/python` via workspace dependency:

```toml
[tool.uv.sources]
hellarch-types = { path = "../libs/data_types/python" }
```

## Running Tests

### Using mise (recommended)

```bash
mise run test:all         # Run all tests
mise run test:api         # Run API tests only
mise run test:integration # Run integration tests only
mise run test:e2e         # Run end-to-end tests
mise run test:parallel    # Run all tests in parallel
mise run test:cov         # Run tests with coverage report
mise run test:smoke       # Run smoke tests only
mise run test:auth        # Run auth-related tests only
mise run test:lint        # Lint test files with ruff
mise run test:format      # Format test files with ruff
```

### Using uv directly

```bash
# All tests
uv run pytest

# Specific test file
uv run pytest api/test_auth.py

# Specific test class
uv run pytest api/test_auth.py::TestHealthEndpoint

# Specific test method
uv run pytest api/test_auth.py::TestValidateEndpoint::test_validate_with_valid_bearer_token

# Parallel execution
uv run pytest -n auto

# Verbose output
uv run pytest -v

# With coverage
uv run pytest --cov --cov-report=term-missing --cov-report=html

# Run tests by marker
uv run pytest -m smoke
uv run pytest -m "auth and not slow"
```

## Test Markers

Tests can be tagged with markers for selective execution:

| Marker | Description |
|--------|-------------|
| `@pytest.mark.smoke` | Quick sanity checks |
| `@pytest.mark.slow` | Long-running tests |
| `@pytest.mark.integration` | Cross-service tests |
| `@pytest.mark.e2e` | End-to-end flow tests |
| `@pytest.mark.auth` | Auth service tests |
| `@pytest.mark.user` | User service tests |
| `@pytest.mark.admin` | Admin functionality tests |

Example usage:

```python
import pytest

@pytest.mark.smoke
@pytest.mark.auth
class TestHealthEndpoint:
    async def test_health_returns_healthy(self, http_client, base_urls):
        response = await http_client.get(f"{base_urls['auth']}/auth/health")
        assert response.status_code == 200
```

## Coverage

Coverage is configured in `pyproject.toml`:

```toml
[tool.coverage.run]
source = ["api", "integration", "e2e"]
branch = true

[tool.coverage.report]
show_missing = true
precision = 2
```

Run with coverage:

```bash
mise run test:cov
# HTML report generated at tests/coverage_html/index.html
```

## Test Structure

```
tests/
├── pyproject.toml       # Dependencies + pytest + coverage config
├── conftest.py          # Global fixtures + plugin registration
├── fixtures/
│   ├── auth.py          # User/session factories with cleanup
│   ├── db.py            # Database connections + cleanup
│   ├── user.py          # User profile/preferences factories
│   ├── redis.py         # Redis fixtures for cache testing
│   ├── rabbitmq.py      # RabbitMQ fixtures for event testing
│   ├── oauth_mocks.py   # OAuth provider mocks (Discord, Google, GitHub)
│   ├── assertions.py    # Custom assertion helpers
│   └── builders.py      # Test data builders with Faker
├── api/                 # API endpoint tests
│   ├── test_auth.py     # Auth service endpoints
│   ├── test_admin.py    # Admin API endpoints
│   └── test_user.py     # User service endpoints
├── integration/         # Cross-service integration tests
└── e2e/                 # End-to-end flow tests
```

## Test Coverage

### Auth Service (`api/test_auth.py`)

| Class | Tests |
|-------|-------|
| `TestHealthEndpoint` | Health check returns healthy |
| `TestValidateEndpoint` | Session token, bearer token, invalid token, no token |
| `TestRefreshEndpoint` | Valid refresh, invalid refresh, expired token |
| `TestMeEndpoint` | Valid session, session cookie, no session, invalid session |

### Admin API (`api/test_admin.py`)

| Class | Tests |
|-------|-------|
| `TestAdminUserEndpoints` | Get user includes permissions and is_active |
| `TestPermissionManagement` | Get/set/grant/revoke permissions, user not found |
| `TestUserActivation` | Deactivate/activate user, deactivate invalidates sessions |
| `TestDeactivatedUserAuthFlow` | Deactivated user session fails, refresh fails |
| `TestPermissionsInAuthResponse` | Validate includes permissions and is_active |

### User Service (`api/test_user.py`)

| Class | Tests |
|-------|-------|
| `TestHealthEndpoint` | Health check returns healthy |
| `TestGetProfileEndpoint` | Profile not found |
| `TestCreateProfileEndpoint` | Create profile, invalid body |
| `TestUpdateProfileEndpoint` | Update not found |
| `TestGetPreferencesEndpoint` | Preferences not found |
| `TestMeEndpoint` | Without user ID header, invalid user ID |

## Fixtures

### HTTP Client

```python
@pytest.fixture
async def http_client() -> httpx.AsyncClient:
    async with httpx.AsyncClient(timeout=30.0) as client:
        yield client
```

### Base URLs

```python
@pytest.fixture(scope="session")
def base_urls() -> dict[str, str]:
    return {
        "gateway": os.getenv("GATEWAY_URL", "http://localhost:5000"),
        "auth": os.getenv("AUTH_URL", "http://localhost:5001"),
        "user": os.getenv("USER_URL", "http://localhost:5002"),
        "admin": os.getenv("ADMIN_URL", "http://localhost:5003"),
        "events": os.getenv("EVENTS_URL", "http://localhost:5005"),
        "files": os.getenv("FILES_URL", "http://localhost:5006"),
        "identity": os.getenv("IDENTITY_URL", "http://localhost:5010"),
    }
```

### JWT Token Creation

```python
@pytest.fixture
def create_access_token(jwt_keys: tuple[str, str]):
    private_key, _ = jwt_keys

    def _create(
        user_id: str,
        email: str = "test@example.com",
        username: str = "testuser",
        expires_in: timedelta = timedelta(hours=1),
    ) -> str:
        claims = {
            "iss": "hellarch-auth",
            "sub": user_id,
            "exp": datetime.now(timezone.utc) + expires_in,
            "uid": user_id,
            "email": email,
            "username": username,
        }
        return jwt.encode(claims, private_key, algorithm="RS256")

    return _create
```

### Test User Factory

Creates a user with a linked OAuth account:

```python
@pytest_asyncio.fixture
async def test_user_factory() -> Callable[..., AuthUser]:
    created_users: list[str] = []

    async def _create(
        email: str = "test@example.com",
        username: str = "testuser",
    ) -> AuthUser:
        async with auth_db_connection() as conn:
            # Creates user + linked_account records
            user = await create_test_user(conn, email, username)
            created_users.append(user.id)
            await conn.commit()
            return user

    yield _create

    # Cleanup (CASCADE deletes linked_accounts, sessions)
    async with auth_db_connection() as conn:
        for user_id in created_users:
            await conn.execute("DELETE FROM users WHERE id = %s", (user_id,))
        await conn.commit()
```

### Database Connections

```python
@asynccontextmanager
async def auth_db_connection() -> AsyncGenerator[psycopg.AsyncConnection, None]:
    async with await psycopg.AsyncConnection.connect(get_auth_db_url()) as conn:
        yield conn
```

### Session Factory

Creates sessions for existing users with automatic cleanup:

```python
@pytest_asyncio.fixture
async def test_session_factory() -> Callable:
    created_sessions: list[str] = []

    async def _create(user_id: str) -> AuthSession:
        async with auth_db_connection() as conn:
            session = await create_test_session(conn, user_id)
            created_sessions.append(session.id)
            await conn.commit()
            return session

    yield _create

    # Cleanup
    async with auth_db_connection() as conn:
        for session_id in created_sessions:
            await conn.execute("DELETE FROM sessions WHERE id = %s", (session_id,))
        await conn.commit()
```

### OAuth Provider Mocks

Mock OAuth token exchange and user info endpoints:

```python
@pytest.fixture
def mock_discord_oauth() -> Generator[tuple[respx.MockRouter, MockOAuthUser], None, None]:
    user = MockOAuthUser(
        id="123456789",
        email="testuser@example.com",
        username="TestUser",
        avatar_url="https://cdn.discordapp.com/avatars/123456789/abc123.png",
    )

    with respx.mock(assert_all_called=False) as mock:
        # Mock token exchange
        mock.post("https://discord.com/api/oauth2/token").respond(
            json={
                "access_token": "mock_discord_access_token",
                "token_type": "Bearer",
                "expires_in": 604800,
            }
        )

        # Mock user info
        mock.get("https://discord.com/api/users/@me").respond(
            json={
                "id": user.id,
                "username": user.username,
                "email": user.email,
            }
        )

        yield mock, user
```

Available mocks: `mock_discord_oauth`, `mock_google_oauth`, `mock_github_oauth`

### Redis Fixtures

Redis fixtures for cache and session testing:

```python
from fixtures.redis import redis_client, redis_helper, RedisTestHelper

class TestCaching:
    async def test_session_caching(
        self,
        redis_helper: RedisTestHelper,
    ):
        # Set a session
        await redis_helper.set_session("session-123", "user-456", ttl=3600)

        # Retrieve it
        user_id = await redis_helper.get_session("session-123")
        assert user_id == "user-456"

    async def test_rate_limiting(
        self,
        redis_helper: RedisTestHelper,
    ):
        # Increment rate limit
        count = await redis_helper.increment_rate_limit("api:user:123", window=60)
        assert count == 1
```

### RabbitMQ Fixtures

RabbitMQ fixtures for event testing with CloudEvents support:

```python
from fixtures.rabbitmq import mq_helper, RabbitMQTestHelper, CloudEvent, create_user_created_event

class TestEventPublishing:
    async def test_user_created_event(
        self,
        mq_helper: RabbitMQTestHelper,
    ):
        # Setup exchange and queue
        await mq_helper.declare_exchange("events", "topic")
        await mq_helper.declare_queue("test-queue")
        await mq_helper.bind_queue("test-queue", "events", "auth.user.*")

        # Publish event
        event = create_user_created_event(
            user_id="123",
            email="test@example.com",
            username="testuser",
        )
        await mq_helper.publish_cloud_event("events", "auth.user.created", event)

        # Consume and verify
        msg = await mq_helper.consume_one("test-queue")
        assert msg is not None
        assert msg.cloud_event.type == "auth.user.created"
```

### User Profile Fixtures

```python
from fixtures.user import test_profile_factory, test_preferences_factory, UserProfile

class TestUserProfile:
    async def test_profile_creation(
        self,
        test_user: AuthUser,
        test_profile_factory,
    ):
        profile = await test_profile_factory(
            auth_user_id=test_user.id,
            email=test_user.email,
            display_name="Test User",
        )
        assert profile.auth_user_id == test_user.id
```

### Custom Assertions

Fluent assertion helpers for cleaner tests:

```python
from fixtures.assertions import assert_response, assert_status, assert_response_model
from pydantic import BaseModel

class UserResponse(BaseModel):
    id: str
    email: str

class TestWithAssertions:
    async def test_fluent_assertions(self, http_client, base_urls, test_user):
        response = await http_client.get(f"{base_urls['auth']}/auth/me")

        # Fluent style
        assert_response(response).status(200).has_keys("user_id", "email")

        # Model validation
        user = assert_response(response).matches(UserResponse)
        assert user.id == test_user.id

    async def test_error_assertions(self, http_client, base_urls):
        response = await http_client.get(f"{base_urls['auth']}/auth/me")

        assert_response(response).status(401).is_error("no session token")
```

### Test Data Builders

Fluent builders for creating test data:

```python
from fixtures.builders import user, session, profile, preferences

class TestWithBuilders:
    async def test_user_builder(self, test_user_factory):
        # Build user data
        user_data = (
            user()
            .with_email("admin@example.com")
            .with_username("admin")
            .with_provider("discord")
            .admin()
            .build()
        )

        # Use with factory
        created_user = await test_user_factory(
            email=user_data.email,
            username=user_data.username,
        )

    async def test_profile_builder(self, test_profile_factory, test_user):
        # Build profile request
        profile_data = (
            profile()
            .for_auth_user(test_user.id)
            .with_display_name("John Doe")
            .with_bio("Software developer")
            .build()
        )

        # Create profile
        await test_profile_factory(**profile_data.__dict__)

    async def test_session_builder(self):
        # Build expired session
        session_data = (
            session()
            .for_user("user-123")
            .expired()
            .with_ip("192.168.1.1")
            .build()
        )
        assert session_data.expires_at < datetime.now(timezone.utc)
```

## Example Tests

### Health Check

```python
class TestHealthEndpoint:
    async def test_health_returns_healthy(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(f"{base_urls['auth']}/auth/health")

        assert response.status_code == 200
        assert response.json() == {"status": "healthy"}
```

### Token Validation

```python
class TestValidateEndpoint:
    async def test_validate_with_valid_bearer_token(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        create_access_token: Callable[..., str],
    ):
        token = create_access_token(
            user_id=test_user.id,
            email=test_user.email,
            username=test_user.username,
        )

        response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"Authorization": f"Bearer {token}"},
        )

        assert response.status_code == 200
        assert response.json()["valid"] is True
```

### Session Authentication

```python
class TestMeEndpoint:
    async def test_me_with_session_cookie(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_session: AuthSession,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/auth/me",
            cookies={"hellarch_session": test_session.token},
        )

        assert response.status_code == 200
        assert response.json()["user_id"] == test_session.user_id
```

### Permission Management

```python
class TestPermissionManagement:
    async def test_grant_permission(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        # Grant permission
        response = await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions/grant",
            json={"permission": "admin.users.write"},
        )
        assert response.status_code == 204

        # Verify it was granted
        get_response = await http_client.get(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/permissions",
        )
        assert get_response.json()["permissions"]["admin.users.write"] is True
```

### User Deactivation Flow

```python
class TestUserActivation:
    async def test_deactivate_invalidates_sessions(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
        test_session_factory: Callable,
    ):
        # Create session for user
        session = await test_session_factory(test_user.id)

        # Verify session is valid
        validate_response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )
        assert validate_response.status_code == 200

        # Deactivate user
        await http_client.post(
            f"{base_urls['auth']}/auth/admin/users/{test_user.id}/deactivate",
        )

        # Session should now be invalid
        validate_response = await http_client.post(
            f"{base_urls['auth']}/auth/validate",
            headers={"X-Session-Token": session.token},
        )
        assert validate_response.status_code == 401
```

## Test Data Flow

```mermaid
sequenceDiagram
    participant Test as Test Method
    participant Fix as Fixtures
    participant DB as Auth DB
    participant API as Auth Service

    Test->>Fix: Request test_user
    Fix->>DB: INSERT users + linked_accounts
    DB-->>Fix: User created
    Fix-->>Test: AuthUser

    Test->>Fix: Request test_session_factory
    Fix-->>Test: Factory function

    Test->>Fix: Call factory(user.id)
    Fix->>DB: INSERT sessions
    DB-->>Fix: Session created
    Fix-->>Test: AuthSession

    Test->>API: POST /auth/validate
    API->>DB: Query session
    DB-->>API: Session data
    API-->>Test: 200 OK

    Note over Fix: After test completes
    Fix->>DB: DELETE sessions
    Fix->>DB: DELETE users (CASCADE)
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `GATEWAY_URL` | `http://localhost:5000` | Gateway base URL |
| `AUTH_URL` | `http://localhost:5001` | Auth service URL |
| `USER_URL` | `http://localhost:5002` | User service URL |
| `ADMIN_URL` | `http://localhost:5003` | Admin console URL |
| `EVENTS_URL` | `http://localhost:5005` | Events service URL |
| `FILES_URL` | `http://localhost:5006` | Files service URL |
| `IDENTITY_URL` | `http://localhost:5010` | Identity service URL |
| `AUTH_DATABASE_URL` | `postgres://...localhost:5434/auth_db` | Auth DB connection |
| `USER_DATABASE_URL` | `postgres://...localhost:5433/user_db` | User DB connection |
| `DATABASE_URL` | `postgres://...localhost:5435/events_db` | Events DB connection |
| `FILES_DATABASE_URL` | `postgres://...localhost:5436/files_db` | Files DB connection |
| `IDENTITY_DATABASE_URL` | `postgres://...localhost:5437/identity_db` | Identity DB connection |
| `REDIS_URL` | `redis://localhost:6379/0` | Redis connection |
| `RABBITMQ_URL` | `amqp://hellarch:hellarch@localhost:5672/` | RabbitMQ connection |

## Database Cleanup

Tests use fixtures that automatically clean up created data:

```python
@pytest_asyncio.fixture
async def clean_auth_db(auth_db: psycopg.AsyncConnection):
    yield auth_db
    await conn.execute("TRUNCATE sessions, linked_accounts, users CASCADE")
```

## pytest Configuration

Configured in `pyproject.toml`:

```toml
[tool.pytest.ini_options]
asyncio_mode = "auto"                        # No need for @pytest.mark.asyncio
asyncio_default_fixture_loop_scope = "function"
testpaths = ["api", "integration", "e2e"]    # Test discovery paths
```

The `asyncio_mode = "auto"` setting means all `async def` test functions are automatically recognized as async tests.

## Adding New Tests

1. **Create test file** in appropriate directory:
   - `api/` - Single service endpoint tests
   - `integration/` - Cross-service tests
   - `e2e/` - Full user flow tests

2. **Use existing fixtures** from `conftest.py` and `fixtures/`:

   **Core:**
   - `http_client` - Async HTTP client
   - `base_urls` - Service URL dict

   **Auth:**
   - `test_user` - User with linked account
   - `test_session` - Session for test_user
   - `test_user_factory` - Create multiple users
   - `test_session_factory` - Create sessions for any user
   - `create_access_token` - Generate JWT access tokens
   - `create_refresh_token` - Generate JWT refresh tokens

   **User Profiles:**
   - `test_profile_factory` - Create user profiles
   - `test_preferences_factory` - Create user preferences

   **Infrastructure:**
   - `redis_client` - Raw Redis connection
   - `redis_helper` - Redis test helper with session/cache/rate-limit methods
   - `mq_channel` - RabbitMQ channel
   - `mq_helper` - RabbitMQ test helper with publish/consume methods

   **OAuth Mocks:**
   - `mock_discord_oauth` - Discord OAuth mock
   - `mock_google_oauth` - Google OAuth mock
   - `mock_github_oauth` - GitHub OAuth mock

3. **Follow the async class pattern**:

```python
from fixtures.auth import AuthUser, AuthSession

class TestMyFeature:
    async def test_success_case(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
        test_user: AuthUser,
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/my/endpoint",
            headers={"X-User-ID": test_user.id},
        )
        assert response.status_code == 200

    async def test_error_case(
        self,
        http_client: httpx.AsyncClient,
        base_urls: dict[str, str],
    ):
        response = await http_client.get(
            f"{base_urls['auth']}/my/endpoint",
        )
        assert response.status_code == 401
```

4. **Add new fixtures** in `fixtures/` if needed:
   - Create factory fixtures for reusable data creation
   - Include cleanup in the fixture teardown
   - Use `@pytest_asyncio.fixture` for async fixtures

## CI Integration

Tests run against live services. Ensure infrastructure is running:

```bash
# Start services
tilt up

# Wait for services to be ready
# (Tilt UI shows status at http://localhost:10350)

# Run tests
mise run test:all

# Or run specific test suite
mise run test:api
```

## Debugging Tips

```bash
# Run single test with output
uv run pytest api/test_auth.py::TestValidateEndpoint::test_validate_with_valid_bearer_token -v -s

# Show local variables on failure
uv run pytest --tb=long

# Drop into debugger on failure
uv run pytest --pdb

# Run tests matching pattern
uv run pytest -k "validate"
```
