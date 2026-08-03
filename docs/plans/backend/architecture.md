# SalesDeliveryBI — Backend Architecture (Clean Architecture + DDD)

**Scope note:** nothing exists yet — the `sales` OLTP schema and the `bi` reporting schema are **both designed and created by this project**, via **EF Core Code First**. The BI dashboards themselves stay **read-only, query-only** (no create/edit AppServices, no entry UI) — but the underlying tables must exist, so this project owns their schema and migrations.

Full DDD aggregates with behavior/invariants still don't apply on the query side (nothing to protect against invalid mutation there). What DDD contributes here: a proper entity model with a shared audit base, Value Objects, Enums-as-domain-concepts, and strict layer isolation.

---

## Entity/Audit Convention (applies to every table)

Every entity inherits a common base:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }              // PK, GUID everywhere — no int/identity PKs
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
```

- `Id` is a GUID on every table, no exceptions — generated app-side (`Guid.NewGuid()`), not DB-side identity/serial.
- `CreatedBy` / `ModifiedBy` are GUIDs referencing the acting user's `Id` — not names, not strings.
- `CreatedDate` / `ModifiedDate` populated automatically via an EF Core `SaveChanges` interceptor — never set manually in application code, so it can't be forgotten or faked.

```csharp
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    // on Added   → CreatedBy = current user id, CreatedDate = now
    // on Modified→ ModifiedBy = current user id, ModifiedDate = now
}
```

---

## Solution Structure

```
SalesDeliveryBI.sln
│
├── src/
│   ├── SalesDeliveryBI.Domain            (no dependencies — pure C#)
│   │   ├── Common/           BaseEntity, Result<T>, Guard clauses
│   │   ├── Entities/         Unit, Buyer, Merchandiser, User, UserUnit, FxRate, Quotation
│   │   ├── Enums/            QuotationStatus, UserRole
│   │   └── ValueObjects/     Money, DateRange
│   │
│   ├── SalesDeliveryBI.Application       (depends on: Domain only)
│   │   ├── Abstractions/     IQuotationQueryRepository, ICacheService, ICurrentUserContext
│   │   ├── Dtos/             QuotationPipelineDto, ConversionDto, AgingDto, QuotationDetailDto
│   │   ├── Queries/          GetQuotationPipelineQuery/Handler, GetConversionQuery/Handler, GetAgingQuery/Handler  (MediatR)
│   │   ├── Behaviors/        UnitSecurityBehavior, CachingBehavior, ValidationBehavior  (MediatR pipeline)
│   │   └── DependencyInjection.cs
│   │
│   ├── SalesDeliveryBI.Infrastructure     (depends on: Application + Domain)
│   │   ├── Persistence/
│   │   │   ├── EfCore/        AppDbContext (Code First, owns `sales` schema entities + migrations),
│   │   │   │                  EntityConfigurations/ (Fluent API per entity), AuditableEntitySaveChangesInterceptor
│   │   │   ├── Migrations/    EF Core migrations for `sales` schema + raw-SQL migrations for `bi` schema (MVs, refresh log, pg_cron)
│   │   │   └── Dapper/        DapperContext, QuotationQueryRepository (reads bi.mv_sales_quotation_summary — read path stays Dapper, not EF, per MV performance rationale)
│   │   ├── Caching/          RedisCacheService (cache-aside + stampede lock)
│   │   ├── Security/         CurrentUserContext (reads JWT claims: user_units, role, sub → Guid userId)
│   │   ├── Jobs/             (optional) CacheWarmupJob — Quartz.NET, triggered post pg_cron refresh
│   │   └── DependencyInjection.cs
│   │
│   └── SalesDeliveryBI.Api                (composition root — depends on Application + Infrastructure)
│       ├── Controllers/      QuotationsController (thin — sends MediatR queries)
│       ├── Middleware/       ExceptionHandling, JwtClaims
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── SalesDeliveryBI.Application.Tests   (query handlers, mocked repo/cache)
    └── SalesDeliveryBI.Infrastructure.Tests (Dapper queries + EF Core migrations against a test Postgres)
```

**Why EF Core for the write/schema side but Dapper for the read side:** EF Core Code First gives a single source of truth for the `sales` schema (entity classes → migrations → actual tables), with the audit interceptor guaranteeing `CreatedBy`/`ModifiedDate` etc. are never missed. But `bi.*` materialized views are flat, denormalized, read-only aggregates with no change-tracking need — Dapper avoids EF's tracking overhead there. Both live in `Infrastructure/Persistence`, cleanly separated by folder.

---

## Dependency Rule (strict, one direction only)

```
Api  →  Infrastructure  →  Application  →  Domain
Api  →  Application  ─────────────────────↗
```

- **Domain** never references anything — entities are plain C# classes/EF-annotation-free (mapping lives in `Infrastructure/EfCore/EntityConfigurations`, not on the entities themselves).
- **Application** defines interfaces (`IQuotationQueryRepository`, `ICacheService`) — never implements them.
- **Infrastructure** implements Application's interfaces (Dapper/Redis/Npgsql) and owns the EF Core `AppDbContext` + migrations — Application never knows Dapper or EF Core exist.
- **Api** is the only project that knows about both Application and Infrastructure — it wires DI in `Program.cs` and exposes HTTP.

---

## Why CQRS-lite (MediatR) fits the read side

- Every one of the 5 dashboard endpoints (`pipeline`, `conversion`, `aging`, `{id}`, `summary`) is a **Query**, not a Command — there's no state to mutate through this API.
- `UnitSecurityBehavior` and `CachingBehavior` as MediatR pipeline behaviors mean the row-level unit check and Redis cache-aside logic run **once, centrally, for every query** — a handler can't accidentally forget to apply them (satisfies the "no bypass path" rule in the repo's `CLAUDE.md`).
- Controllers stay one-liners: `return Ok(await _mediator.Send(new GetQuotationPipelineQuery(unitId, from, to)));`

---

## Request Flow Example — `GET /api/sales/quotations/pipeline?unitId={guid}`

1. Controller receives request, extracts JWT claims, sends `GetQuotationPipelineQuery(unitId, from, to)` via MediatR.
2. `UnitSecurityBehavior` validates `unitId` against caller's `user_units` claim → throws `ForbiddenException` (mapped to `403`) if outside assignment.
3. `CachingBehavior` checks Redis for `bi:sales:quotation:pipeline:unit:{unitId}:{date}` → cache hit short-circuits and returns immediately.
4. On miss: handler calls `IQuotationQueryRepository.GetPipelineSummaryAsync(unitId)` → Dapper query against `bi.mv_sales_quotation_summary`.
5. Result mapped to DTO (includes `lastRefresh` from `bi.mv_refresh_log`), cached by `CachingBehavior`, returned to controller.

**Background/scheduled side (outside the request path):**
- `pg_cron` inside Postgres refreshes each MV on its own cadence (3–15 min depending on the view) via `REFRESH MATERIALIZED VIEW CONCURRENTLY`.
- Optional: a Quartz.NET job for cache warm-up right after refresh, if dashboards should be pre-warmed instead of first-request-pays-the-cost.

---

## Logging — Serilog (file sink)

- Packages: `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`, `Serilog.Enrichers.CorrelationId` (or `Serilog.Enrichers.Span` if tying into `traceId`).
- Configured as a two-stage logger in `Program.cs` — a minimal bootstrap logger active before configuration loads, replaced by the fully-configured logger once `appsettings.json` is read:
  ```csharp
  Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateBootstrapLogger();

  builder.Host.UseSerilog((context, services, config) => config
      .ReadFrom.Configuration(context.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .WriteTo.File(
          path: "logs/salesdeliverybi-.log",
          rollingInterval: RollingInterval.Day,
          retainedFileCountLimit: 30,
          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"));
  ```
- `appsettings.json` carries the `Serilog` section (minimum level, overrides per namespace) so log verbosity is configurable per environment without a rebuild.
- `app.UseSerilogRequestLogging()` in the middleware pipeline — logs one structured line per HTTP request (method, path, status code, elapsed ms) instead of the framework's default noisy per-request logging.
- **Structured logging only** — always `_logger.LogInformation("Quotation pipeline requested for unit {UnitId}", unitId)`, never string interpolation (`$"..."`) — keeps logs queryable/filterable in the file sink.
- Never log PII or full JWTs; log `sub` (user GUID) and `unitId`, not names/emails, when tracing a request.
- Exceptions logged with full context (`unitId`, `userId`, query name) at the point they're translated to a Problem Details response (see `api-contract.md`'s `traceId` field) — the file log's timestamp + traceId is how a support request gets traced back to what happened server-side.

---

## Decisions Confirmed

- **MediatR / CQRS-lite** for the query side.
- **Serilog with rolling file sink** for all logging.
- **EF Core Code First** for the `sales` OLTP schema — entities, migrations, audit interceptor.
- **GUID primary keys** everywhere; `CreatedBy`/`ModifiedBy` as GUID references, `CreatedDate`/`ModifiedDate` on every table.
