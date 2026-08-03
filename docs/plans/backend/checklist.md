# SalesDeliveryBI — Backend Implementation Checklist

Phased build order. Each phase should be functionally complete and buildable before moving to the next — don't skip ahead with stubs.

---

## Phase 0 — Project Setup

- [x] Create solution `SalesDeliveryBI.sln`
- [x] Create projects: `Domain`, `Application`, `Infrastructure`, `Api`, `Application.Tests`, `Infrastructure.Tests`
- [x] Wire project references per the dependency rule (`Api → Infrastructure → Application → Domain`, `Api → Application`)
- [x] Add `.editorconfig` / analyzer rules (nullable reference types enabled, warnings-as-errors for the non-negotiables in `CLAUDE.md`)
- [x] Provision local PostgreSQL 15+ and Redis instances (docker-compose for dev)
- [x] `CREATE EXTENSION pg_cron;` on the dev database, confirm `shared_preload_libraries` includes `pg_cron`

## Phase 1 — Domain Layer

- [x] `BaseEntity` (Id: Guid, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate) — `CreatedBy`/`ModifiedBy` are bare GUIDs with **no local FK**; the identity they reference lives in the separate Identity service, not this schema
- [x] Entities: `Unit`, `Buyer`, `Merchandiser`, `FxRate`, `Quotation` — **no `User`/`UserUnit` entities here**, those are owned by the Identity service (see `docs/plans/security/security-plan.md` §6)
- [x] Enums: `QuotationStatus`
- [ ] Value Objects: `Money`, `DateRange` (if used) — skipped: nothing in Phase 1 consumes them (`Quotation` uses raw `CurrencyCode`/`Value` per `database/schema-plan.md`); revisit if a later phase needs them
- [x] `Result<T>` / guard clause helpers in `Common/`

## Phase 2 — Database: EF Core Code First (`sales` schema)

- [x] `AppDbContext` in `Infrastructure/Persistence/EfCore`
- [x] Entity Fluent API configurations (one file per entity) — table/schema mapping, unique constraints (`QuotationNo`), enum-as-string conversions
- [x] `AuditableEntitySaveChangesInterceptor` — auto-populates `CreatedBy`/`CreatedDate`/`ModifiedBy`/`ModifiedDate`
- [x] `dotnet ef migrations add InitialCreate` — verify generated SQL creates all `sales` tables with GUID PKs
- [x] Hand-written migration `AddBiSchemaAndViews` — `CREATE SCHEMA bi`, all 3 materialized views + unique indexes, `bi.mv_refresh_log`, `pg_cron` schedule statements
- [x] `dotnet ef database update` — confirm both schemas exist end-to-end on a clean database

## Phase 3 — Seed Data

- [x] `seed-quotations.json` (or equivalent) — 30-row dataset per `docs/plans/database/seed-data.md`
- [x] `DatabaseSeeder` class — idempotent upsert by natural key
- [x] Wire seeder to run only when `IsDevelopment()`
- [x] Manually refresh MVs once after first seed run, confirm dashboards would have data

## Phase 4 — Application Layer (plain AppServices, no MediatR)

- [ ] Abstractions: `IQuotationRepository`, `ICacheService`, `ICurrentUserContext`, `IUnitAccessGuard`
- [ ] DTOs: `QuotationPipelineDto`, `ConversionDto`, `AgingDto`, `QuotationDetailDto`, `QuotationSummaryDto`
- [ ] `QuotationAppService` with 5 methods: `GetPipelineAsync`, `GetConversionAsync`, `GetAgingAsync`, `GetByIdAsync`, `GetSummaryAsync` — each explicitly calls `IUnitAccessGuard.Validate` then `ICacheService.GetOrSetAsync`
- [ ] Register `QuotationAppService` + abstractions in `Application/DependencyInjection.cs`

## Phase 5 — Infrastructure: Persistence (Dapper read side)

- [ ] `DapperContext` (Npgsql connection factory)
- [ ] `QuotationRepository` implementing `IQuotationRepository` — queries against `bi.mv_sales_quotation_summary` and the 2 supporting MVs
- [ ] Confirm every method is only ever called after `IUnitAccessGuard.Validate` has run (code review checklist item, since there's no pipeline enforcing it)
- [ ] Unit tests against a real test Postgres instance (not mocked) for each query method

## Phase 6 — Infrastructure: Caching (Redis)

- [ ] `RedisCacheService` implementing `ICacheService` — cache-aside get/set
- [ ] Cache key builder — dashboard name + hash of filter params, per `docs/plans/backend/architecture.md` key patterns
- [ ] Stampede protection — `SET key NX PX 2000` short lock around recompute on cache miss
- [ ] TTLs wired per dashboard (3–5 min pipeline, 10–15 min conversion, 5 min aging)

## Phase 7 — Infrastructure: Security

- [ ] Confirm the Identity service's JWT contract is available to test against (`sub`, `permissions[]`, `user_units[]` — see `docs/plans/security/security-plan.md` §2). This is an external dependency, not built in this repo.
- [ ] `CurrentUserContext` implementing `ICurrentUserContext` — reads `sub`, `permissions`, `user_units` from JWT claims
- [ ] ASP.NET Core policies: `QuotationRead`, `QuotationReadAllUnits` (permission-claim based, **not** role-name based) per `docs/plans/security/security-plan.md`
- [ ] `UnitAccessGuard` implementing `IUnitAccessGuard` — checks for `bi.quotation.viewAllUnits` permission claim to decide all-units vs. assigned-units-only scoping
- [ ] `ForbiddenAccessException` → mapped to HTTP `403` (not a silent empty result)
- [ ] `{id}` endpoint: out-of-scope quotation → `404`, not `403` (avoid confirming existence)

## Phase 8 — Infrastructure: Background Jobs

- [ ] `pg_cron` schedules verified running (check `cron.job_run_details` after first cycle)
- [ ] Quartz.NET packages added (`Quartz`, `Quartz.Extensions.Hosting`)
- [ ] `CacheWarmupJob` — one trigger per MV, offset 10–15s after each `pg_cron` cadence
- [ ] Warm-up job uses the exact same cache keys as `CachingBehavior` (no separate key scheme)
- [ ] Job failures logged, never throw past the job boundary

## Phase 9 — Logging (Serilog)

- [ ] Bootstrap logger + full `UseSerilog` configuration in `Program.cs`
- [ ] Rolling file sink (`logs/salesdeliverybi-.log`, daily, 30-day retention)
- [ ] `Serilog` section in `appsettings.json` (min level + per-namespace overrides)
- [ ] `UseSerilogRequestLogging()` middleware
- [ ] Confirm no PII/JWT values ever logged — only `sub`/`unitId`

## Phase 10 — API Layer

- [ ] `QuotationsController` — 5 thin endpoints (`pipeline`, `conversion`, `aging`, `{id}`, `summary`)
- [ ] JWT bearer authentication configured in `Program.cs`
- [ ] Global exception-handling middleware → RFC 7807 Problem Details responses
- [ ] Response shape matches `docs/plans/api-contract.md` exactly (including `lastRefresh` on every response)
- [ ] Swagger/OpenAPI spec generated and matches the contract doc

## Phase 11 — Testing

- [ ] `Application.Tests` — `QuotationAppService` methods with mocked repository/cache/guard, including `IUnitAccessGuard` denial paths (test both: caller missing `bi.quotation.view` entirely, and caller with it but requesting a unit outside `user_units`)
- [ ] `Infrastructure.Tests` — Dapper queries + EF Core migrations against a real test Postgres
- [ ] Integration test: full request → `403` when `unitId` outside caller's `user_units` and caller lacks `bi.quotation.viewAllUnits`
- [ ] Integration test: caller with `bi.quotation.viewAllUnits` can query any unit, including `unitId=null` for a global view
- [ ] Integration test: cache hit vs. miss path returns identical payload

## Phase 12 — Hardening / Verification

- [ ] Load test: concurrent MV refresh + concurrent dashboard reads (confirm `CONCURRENTLY` prevents lock contention)
- [ ] Load test: cache stampede scenario — simulate many concurrent requests on a cold cache key
- [ ] Security review: attempt row-level bypass on every endpoint's `unitId` param
- [ ] Confirm FX/currency values are correct against a manually verified sample (Finance sign-off)
- [ ] Confirm "Data as of {lastRefresh}" renders correctly end-to-end (backend value reaches frontend unmodified)
