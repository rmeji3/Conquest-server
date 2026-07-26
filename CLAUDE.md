# Ping Server — Code Guidelines

ASP.NET Core (.NET 9) API with EF Core. **Postgres in production, SQLite in tests** —
keep queries provider-agnostic. Before writing code, read the neighboring files in
the folder you're touching and match their patterns exactly.

## Where things go

| Concern | Location | Notes |
|---|---|---|
| Controllers | `Controllers/<Domain>/` | Thin: auth/claims extraction, model binding, mapping service exceptions to status codes. No business logic. |
| Services | `Services/<Domain>/` | `IXService` interface + `XService` implementation (primary constructors, registered in `Program.cs`). All business logic and EF queries live here. |
| DTOs | `Dtos/<Domain>/` | `record` types. Input DTOs carry DataAnnotations validation; output DTOs are positional records projected in queries. |
| Entities | `Models/<Domain>/` | EF entities + domain enums. |
| Data | `Data/App/AppDbContext.cs` | Entity configuration in `OnModelCreating`. |
| Middleware | `Middleware/` | Global exception handler already exists. |
| Tests | `Tests/Ping.Tests/` | xUnit against SQLite. |

## House patterns

- **Controllers**: get the user via `User.FindFirstValue(ClaimTypes.NameIdentifier)`
  and return `Unauthorized()` if null; `[Authorize]` on the controller,
  `[Authorize(Roles = "Admin")]` on admin endpoints; both versioned route attributes
  (`api/[controller]` and `api/v{version:apiVersion}/[controller]`). Map
  `KeyNotFoundException` → `NotFound`, `InvalidOperationException`/`ArgumentException`
  → `BadRequest` with try/catch per action.
- **Services throw, controllers translate.** Services never return `ActionResult`.
- **Lists are paginated** with `PaginatedResult<T>.CreateAsync(query, pageNumber, pageSize)`.
- **Project to DTOs inside the query** (`Select(x => new XDto(...))`) — don't return
  entities from list/read endpoints, and don't load whole entities to map in memory.
  When a row references a user by bare id, enrich with a left join (see
  `ReportService.GetReportsAsync`).
- XML doc comments (`/// <summary>`) on controller actions.

## API compatibility — the prime directive

The mobile app ships through Apple review and updates slowly; the server deploys
freely. Every server change must keep already-shipped clients working:

- Never rename or remove response fields, routes, or query params the app uses.
  Additive changes only; new required inputs need defaults.
- **Enums are wire contracts.** They serialize as strings (`JsonStringEnumConverter`)
  but are stored/sent as ints from the app — pin explicit numeric values and never
  reorder or reuse them (see `ReportTargetType`, where 7 is intentionally unused).
- Schema changes go through EF migrations; prefer additive nullable columns.

## Database

- No provider-specific SQL/functions unless guarded by
  `Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"` (see the FTS
  setup in `AppDbContext`) — tests run on SQLite and must still pass.
- Fire-and-forget background work must create its own DI scope
  (`IServiceScopeFactory`); never capture a request-scoped `DbContext` in a task
  that outlives the request.

## Testing — required, not optional

For any feature or service method you touch: check `Tests/Ping.Tests` for existing
coverage of it first.

- **Coverage exists** → update/extend it to reflect the new behavior; don't leave it
  asserting the old behavior.
- **No coverage exists** → write it. New service methods, new controller behavior,
  and bug fixes (a fix without a regression test isn't done) all need tests.

Match the test fixtures' `JsonStringEnumConverter` setup when asserting on API
responses, and keep tests provider-agnostic (SQLite in `Tests/Ping.Tests`, Postgres
in prod — see the Database section above).

## Review the code you touch, not just the task

Before finishing, re-read the file(s) you changed (and the immediate surrounding
code, not just your diff) and flag anything you notice, even if out of scope for the
requested change:

- **Refactoring**: does it match the house patterns above (thin controllers, logic
  in services, DTO projection in-query, etc.)? Call out anything that doesn't and
  either fix it if it's small, or say so explicitly if it's a larger change.
- **Security**: missing `[Authorize]`, unvalidated input, a query building
  provider-specific SQL without the `Database.ProviderName` guard, secrets or PII
  logged, an entity returned directly from a list/read endpoint instead of a
  projected DTO.
- **Performance/correctness**: N+1 queries, loading whole entities to map in memory,
  missing pagination on a list endpoint, a fire-and-forget task capturing a
  request-scoped `DbContext` instead of a fresh DI scope.

Report findings plainly at the end of your response — don't silently fix things
outside the requested scope without calling them out, and don't stay quiet about
something you noticed just because it wasn't asked about.

## Before you're done

```sh
dotnet build --nologo -v q   # zero warnings expected
dotnet test  --nologo -v q   # full suite, runs on SQLite
```
