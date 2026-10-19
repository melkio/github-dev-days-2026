# TaskFlow architecture

TaskFlow v1.0 is one ASP.NET Core Minimal API project plus one integration-test project. The API is organized by feature; endpoint handlers perform HTTP binding/validation, while `TaskService` owns task workflows and EF Core access. User read endpoints query through `AppDbContext` directly because no write workflow exists.

```text
HTTP client
    |
    v
Minimal API endpoints ---- TaskService (task workflows)
    |                              |
    +----------- AppDbContext -----+
                       |
                  EF Core / SQLite
                       |
             migrations + idempotent seed
```

Request and response DTOs are separate from EF entities. SQLite stores task status as its enum name and stores UTC timestamps in round-trip ISO text so ordering is consistent. User email uniqueness uses SQLite's case-insensitive `NOCASE` collation. `TimeProvider` supplies application timestamps and is replaced by a fixed clock in integration tests.

On Development startup, the API applies pending migrations and inserts any missing seed records. The test factory instead uses a separately opened SQLite in-memory connection per test, applies the same migrations, and seeds that isolated database. No Task–User relation or v1.1 fields are present.
