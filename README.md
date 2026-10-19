# TaskFlow v1.0

TaskFlow is a small REST API for creating, reading, updating, changing the status of, and deleting team tasks. Users are seeded and read-only. This baseline deliberately excludes a business frontend, task assignments, due dates, notifications, authentication, paging, and full-text search.

## Prerequisites

- .NET SDK 10
- Bash and `curl` for the helper scripts

SQLite is embedded; no database server, container, cloud service, or external credentials are required.

## Run locally

Run these commands from the repository root:

```bash
dotnet tool restore
dotnet restore
dotnet build
dotnet run --project src/TaskFlow.Api
```

The Development launch profile uses `http://localhost:5000`. The local database is `src/TaskFlow.Api/data/taskflow.db`, resolved relative to the API project content root and created automatically. On Development startup, pending EF Core migrations are applied and the seed is inserted idempotently. Browse the interactive OpenAPI UI at <http://localhost:5000/swagger>; the underlying JSON document is at <http://localhost:5000/openapi/v1.json>. Both documentation endpoints are enabled only in Development. The Swagger UI assets are served by the application, without a CDN dependency.

## Tests

```bash
dotnet test
```

Integration tests use `WebApplicationFactory<Program>` and a dedicated, open SQLite in-memory connection per test. They apply the checked-in migrations and seed into that isolated database; they do not use or modify the development database.

## API examples

Create a task:

```bash
curl -i -X POST http://localhost:5000/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"title":"Prepare the demo","description":"Verify the baseline tests"}'
```

List all tasks or filter by status (`Todo`, `InProgress`, or `Done`):

```bash
curl http://localhost:5000/api/tasks
curl 'http://localhost:5000/api/tasks?status=InProgress'
```

Change a task's status using its returned GUID:

```bash
curl -i -X PATCH http://localhost:5000/api/tasks/<task-guid>/status \
  -H 'Content-Type: application/json' \
  -d '{"status":"Done"}'
```

Read seeded users, including inactive users:

```bash
curl http://localhost:5000/api/users
```

Other task operations are `GET /api/tasks/{id}`, `PUT /api/tasks/{id}` (title and description only), and `DELETE /api/tasks/{id}`. Health is available at `GET /health`.

## Reset the local database

Stop the API first. Then run:

```bash
./scripts/reset-db.sh
```

The script removes only the known development SQLite database and its `-wal`/`-shm` sidecars, then recreates and seeds it through EF migrations. Do not run it while the API is using the database.

The smoke test targets an already-running API and does not launch one:

```bash
./scripts/smoke-test.sh
BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh
```

## Repository structure and decisions

- `src/TaskFlow.Api/Features/Tasks` contains task entities, DTOs, endpoints, and task application logic.
- `src/TaskFlow.Api/Features/Users` contains the read-only user API and DTOs.
- `src/TaskFlow.Api/Persistence` contains EF Core mapping, seed data, and migrations.
- `tests/TaskFlow.IntegrationTests` contains API-level tests over isolated in-memory SQLite.
- `docs/architecture.md` summarizes the component layout.

Validation is explicit in endpoint handlers to keep request rules close to the HTTP contract. UTC timestamps are stored as round-trip ISO text so SQLite can order them lexically; emails use SQLite `NOCASE` collation for case-insensitive uniqueness. Seed identifiers and timestamps are fixed for repeatable demos. The local `dotnet-ef` tool is pinned in `.config/dotnet-tools.json`. The development-only Swagger UI is an additional documentation tool; it does not add a business frontend or change the v1.0 API scope.
