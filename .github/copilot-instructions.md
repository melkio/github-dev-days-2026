# Repository conventions

- Keep the API in the single `TaskFlow.Api` project and organize code by feature.
- Keep EF entities separate from request and response DTOs; do not add out-of-scope v1.1 fields.
- Use async EF operations with propagated cancellation tokens and `AsNoTracking()` for reads.
- Validate request data explicitly and return `ValidationProblemDetails` for invalid input.
- Use `TimeProvider` for application timestamps and SQLite migrations for schema changes.
- Add integration coverage through `WebApplicationFactory` with an isolated SQLite in-memory connection.
