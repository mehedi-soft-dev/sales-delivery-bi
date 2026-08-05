# SalesDeliveryBI — Backend Architecture (Clean Architecture + DDD)

**Scope note:** nothing exists yet — the `sales` OLTP schema and the `bi` reporting schema are **both designed and created by this project**, via **EF Core Code First**. The BI dashboards themselves stay **read-only, query-only** (no create/edit AppServices, no entry UI) — but the underlying tables must exist, so this project owns their schema and migrations.

**Decision: this is a demo project, no other developers will touch it — MediatR/CQRS was overkill for 5 read-only endpoints and has been dropped.** Plain **AppService classes** instead: each dashboard method explicitly calls the unit-security check and the cache-aside helper itself. Simpler to read and step through; the cost is that "no bypass path" is now enforced by convention (every method must call the two helpers) rather than structurally guaranteed by a pipeline — acceptable here since one person owns the whole codebase.

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
│   │   ├── Entities/         Unit, Buyer, Merchandiser, User, Role, RolePermission, UserUnit, RoleNames, FxRate, Quotation
│   │   ├── Enums/            QuotationStatus
│   │   └── ValueObjects/     Money, DateRange
│   │
│   ├── SalesDeliveryBI.Application       (depends on: Domain only)
│   │   ├── Abstractions/     IQuotationRepository, ICacheService, ICurrentUserContext, IUnitAccessGuard,
│   │   │                     IUserRepository, IPasswordHasher, IJwtTokenGenerator (real login, docs/plans/security/security-plan.md §6)
│   │   ├── Dtos/             QuotationPipelineDto, ConversionDto, AgingDto, QuotationDetailDto, QuotationSummaryDto,
│   │   │                     LoginRequestDto, LoginResponseDto
│   │   ├── Services/         QuotationAppService (GetPipelineAsync, GetConversionAsync, GetAgingAsync, GetByIdAsync, GetSummaryAsync),
│   │   │                     AuthAppService (LoginAsync)
│   │   └── DependencyInjection.cs
│   │
│   ├── SalesDeliveryBI.Infrastructure     (depends on: Application + Domain)
│   │   ├── Persistence/
│   │   │   ├── EfCore/        AppDbContext (Code First, owns `sales` schema entities + migrations),
│   │   │   │                  EntityConfigurations/ (Fluent API per entity), AuditableEntitySaveChangesInterceptor,
│   │   │   │                  UserRepository (EF Core — Users isn't a `bi.*` view, so this stays EF not Dapper;
│   │   │   │                  eager-loads Role.RolePermissions + UserUnits in one round trip)
│   │   │   ├── Migrations/    EF Core migrations for `sales` schema + raw-SQL migrations for `bi` schema (MVs, refresh log, pg_cron)
│   │   │   └── Dapper/        DapperContext, QuotationRepository (reads bi.mv_sales_quotation_summary — read path stays Dapper, not EF, per MV performance rationale)
│   │   ├── Caching/          RedisCacheService (cache-aside + stampede lock)
│   │   ├── Security/         CurrentUserContext (reads JWT claims: sub, permissions, user_units), UnitAccessGuard (validates/resolves unitId against the claims),
│   │   │                     PasswordHasher (BCrypt.Net-Next), JwtTokenGenerator (reads permission codes off the
│   │   │                     loaded Role.RolePermissions — no separate lookup)
│   │   ├── Jobs/             CacheWarmupJob — Quartz.NET, triggered post pg_cron refresh
│   │   └── DependencyInjection.cs
│   │
│   └── SalesDeliveryBI.Api                (composition root — depends on Application + Infrastructure)
│       ├── Controllers/      QuotationsController (thin — calls QuotationAppService directly, no mediator indirection),
│       │                     AuthController (POST /api/auth/login — anonymous, no [Authorize])
│       ├── Middleware/       ExceptionHandling, JwtClaims
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── SalesDeliveryBI.Application.Tests   (QuotationAppService, mocked repo/cache/guard)
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
- **Application** defines interfaces (`IQuotationRepository`, `ICacheService`, `IUnitAccessGuard`) — never implements them.
- **Infrastructure** implements Application's interfaces (Dapper/Redis/Npgsql) and owns the EF Core `AppDbContext` + migrations — Application never knows Dapper or EF Core exist.
- **Api** is the only project that knows about both Application and Infrastructure — it wires DI in `Program.cs` and exposes HTTP.

---

## Why Plain AppServices (no MediatR)

- Demo project, single developer, 5 read-only endpoints — MediatR's Query/Handler-per-endpoint ceremony and pipeline behaviors add indirection without a payoff here.
- `QuotationAppService` calls `IUnitAccessGuard` and `ICacheService` explicitly at the top of each method — same two things the old pipeline behaviors did, just as plain method calls instead of framework magic. Easier to step through in a debugger, one less package to explain.
- Controllers stay one-liners: `return Ok(await _quotationAppService.GetPipelineAsync(unitId));`
- **Known tradeoff:** since there's no pipeline forcing it, every new AppService method must remember to call the guard + cache helpers itself — there's no structural guarantee like MediatR behaviors gave. Acceptable for a single-owner demo; would need revisiting if this ever grows past that.

---

## Request Flow Example — `GET /api/sales/quotations/pipeline?unitId={guid}`

1. Controller receives request, calls `QuotationAppService.GetPipelineAsync(unitId)`.
2. `QuotationAppService` calls `IUnitAccessGuard.Validate(unitId)` — reads `permissions`/`user_units` off `ICurrentUserContext`, resolves the effective unit scope, throws `ForbiddenAccessException` (mapped to `403`) if the requested unit is outside the caller's assignment and they lack `bi.quotation.viewAllUnits`.
3. `QuotationAppService` calls `ICacheService.GetOrSetAsync("bi:sales:quotation:pipeline:unit:{unitId}:{date}", ttl, factory)` — cache hit returns immediately.
4. On miss, the factory calls `IQuotationRepository.GetPipelineSummaryAsync(unitId)` → Dapper query against `bi.mv_sales_quotation_summary`.
5. Result mapped to DTO (includes `lastRefresh` from `bi.mv_refresh_log`), cached, returned to controller.

**Background/scheduled side (outside the request path):**
- `pg_cron` inside Postgres refreshes each MV on its own cadence (3–15 min depending on the view) via `REFRESH MATERIALIZED VIEW CONCURRENTLY`. This is a Postgres extension, not a .NET package — requires `shared_preload_libraries = 'pg_cron'` in `postgresql.conf` and `CREATE EXTENSION pg_cron;`.
- **Confirmed: Quartz.NET** for the cache warm-up job — runs inside the API process, fires shortly after each MV's `pg_cron` cadence, and pre-populates the Redis keys so the first dashboard request after a refresh never pays the cache-miss cost.

### Cache Warm-up Job (Quartz.NET)

Packages:
```
Quartz
Quartz.Extensions.Hosting
```

- Registered in `Infrastructure/Jobs/CacheWarmupJob.cs`, wired via `services.AddQuartzHostedService()` in `Infrastructure/DependencyInjection.cs`.
- One trigger per MV, offset ~10–15s after that MV's `pg_cron` cadence (e.g. quotation summary refreshes every 3 min → warm-up trigger at `*/3 * * * *` + 15s offset):

  | MV / Dashboard | `pg_cron` Cadence | Quartz Warm-up Trigger |
  |---|---|---|
  | Pipeline + Aging (`mv_sales_quotation_summary`) | 3 min | every 3 min, +15s offset |
  | Conversion (`mv_quotation_conversion_rate`) | 15 min | every 15 min, +15s offset |

  **Revised (discussed with the user):** Aging used to warm off a separate `mv_quotation_pipeline_daily`-keyed trigger on a 15-min cadence, even though Aging actually reads `mv_sales_quotation_summary` (same as Pipeline) — a mismatch left over from that MV, which is now dropped (`schema-plan.md` §2). Aging now warms off the same 3-min Pipeline trigger (`CacheWarmupJob.WarmPipelineAndAgingAsync`), correctly matching its real data dependency.

- The job calls the same repository methods the API would (`IQuotationRepository`), populating Redis under the exact same cache keys `QuotationAppService` uses via `ICacheService` — no separate warm-up-specific key scheme, or the API would still miss on first request.
- Job failures are logged (Serilog) but never throw past the job boundary — a failed warm-up just means the next real request pays the miss cost once; it must not crash the host.

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

- **Plain AppService classes** for the query side — no MediatR/CQRS (dropped as overkill for a single-owner demo with 5 read-only endpoints).
- **Serilog with rolling file sink** for all logging.
- **EF Core Code First** for the `sales` OLTP schema — entities, migrations, audit interceptor.
- **GUID primary keys** everywhere; `CreatedBy`/`ModifiedBy` as GUID references, `CreatedDate`/`ModifiedDate` on every table.
- **Quartz.NET** for the cache warm-up job, triggered on an offset schedule after each MV's `pg_cron` refresh.
