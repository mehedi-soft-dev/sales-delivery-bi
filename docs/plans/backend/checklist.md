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

- [x] Abstractions: `IQuotationRepository`, `ICacheService`, `ICurrentUserContext`, `IUnitAccessGuard`
- [x] DTOs: `QuotationPipelineDto`, `ConversionDto`, `AgingDto`, `QuotationDetailDto`, `QuotationSummaryDto` (plus `DashboardResponse<T>` wrapping every one with `lastRefresh`, per `api-contract.md`)
- [x] `QuotationAppService` with 5 methods: `GetPipelineAsync`, `GetConversionAsync`, `GetAgingAsync`, `GetByIdAsync`, `GetSummaryAsync` — each explicitly calls `IUnitAccessGuard.Validate` then `ICacheService.GetOrSetAsync`
- [x] Register `QuotationAppService` + abstractions in `Application/DependencyInjection.cs` — abstractions registered from `Infrastructure/DependencyInjection.cs` once their implementations exist (Application has no concrete `ICacheService`/`IUnitAccessGuard`/`IQuotationRepository` of its own)

## Phase 5 — Infrastructure: Persistence (Dapper read side)

- [x] `DapperContext` (Npgsql connection factory)
- [x] `QuotationRepository` implementing `IQuotationRepository` — queries against `bi.mv_sales_quotation_summary` and the 2 supporting MVs
- [x] Confirm every method is only ever called after `IUnitAccessGuard.Validate` has run — `QuotationAppService` is the only caller and always validates first (see Phase 4)
- [x] Unit tests against a real test Postgres instance (not mocked) for each query method — `QuotationRepositoryTests` (6 tests, all passing against the seeded dev DB)

## Phase 6 — Infrastructure: Caching (Redis)

- [x] `RedisCacheService` implementing `ICacheService` — cache-aside get/set
- [x] Cache key builder — dashboard name + hash of filter params, per `docs/plans/backend/architecture.md` key patterns (`Application/Common/CacheKeys.cs`, Phase 4)
- [x] Stampede protection — `SET key NX PX 2000` short lock around recompute on cache miss (Lua compare-and-delete release + bounded wait with fail-open fallback; verified with 10 concurrent cold-cache callers → factory ran once)
- [x] TTLs wired per dashboard (3–5 min pipeline, 10–15 min conversion, 5 min aging) — set in `QuotationAppService` (Phase 4)

## Phase 7 — Infrastructure: Security

- [ ] Confirm the Identity service's JWT contract is available to test against (`sub`, `permissions[]`, `user_units[]` — see `docs/plans/security/security-plan.md` §2). This is an external dependency, not built in this repo — genuinely still not available; left unchecked rather than assumed.
- [x] `CurrentUserContext` implementing `ICurrentUserContext` — reads `sub`, `permissions`, `user_units` from JWT claims; falls back to the fixed system identity outside an HTTP request (startup seeding, Phase 8 jobs)
- [x] ASP.NET Core policies: `QuotationRead`, `QuotationReadAllUnits` (permission-claim based, **not** role-name based) per `docs/plans/security/security-plan.md` — registered via `AddQuotationAuthorizationPolicies()`; corrected the claim type to `permissions` (plural) to match the JWT contract in §2 — §3's own code sample used the singular `permission`, which doesn't match what's actually in the token. Applied via `[Authorize(Policy = ...)]` on `QuotationsController` (Phase 10) — verified end-to-end (401 no token, 403 missing permission).
- [x] `UnitAccessGuard` implementing `IUnitAccessGuard` — checks for `bi.quotation.viewAllUnits` permission claim to decide all-units vs. assigned-units-only scoping; unit-tested for all 5 branches (unrestricted, restricted-to-requested, restricted-to-assigned, in-scope, forbidden)
- [x] `ForbiddenAccessException` → mapped to HTTP `403` — `GlobalExceptionHandler` (Phase 10); verified end-to-end with a real out-of-scope-unit request returning 403 + Problem Details + `traceId`
- [x] `{id}` endpoint: out-of-scope quotation → `404`, not `403` — verified end-to-end (`GetById_UnknownQuotation_Returns404ProblemDetails`)

## Phase 8 — Infrastructure: Background Jobs

- [x] `pg_cron` schedules verified running (check `cron.job_run_details` after first cycle) — all 3 jobs `succeeded` repeatedly on their own cadence
- [x] Quartz.NET packages added (`Quartz`, `Quartz.Extensions.Hosting`)
- [x] `CacheWarmupJob` — one trigger per MV, offset 15s after each `pg_cron` cadence (`15 0/3 * * * ?` / `15 0/15 * * * ?`)
- [x] Warm-up job uses the exact same cache keys as `QuotationAppService`'s cache-aside calls — required making `CacheKeys`/TTLs public/shared (were private to `QuotationAppService`); verified directly (`CacheWarmupJobTests`, 5 tests against real dev Redis/Postgres) rather than waiting on real cron timing
- [x] Job failures logged, never throw past the job boundary — verified with a repository that always throws; `WarmUpAsync` swallows it and logs

## Phase 9 — Logging (Serilog)

- [x] Bootstrap logger + full `UseSerilog` configuration in `Program.cs`
- [x] Rolling file sink (`logs/salesdeliverybi-.log`, daily, 30-day retention) — verified live, correct output template
- [x] `Serilog` section in `appsettings.json` (min level + per-namespace overrides)
- [x] `UseSerilogRequestLogging()` middleware — verified live: `HTTP GET /nonexistent-path responded 404 in 5.34 ms`
- [x] Confirm no PII/JWT values ever logged — only `sub`/`unitId` — `GlobalExceptionHandler` (Phase 10) logs `sub` (read directly off `HttpContext.User`, not the full token) on 403/500 responses; every other `Log*(` call logs only cache keys, MV names, or aggregate counts. No name/email/JWT anywhere. (Caught a real bug writing this: injecting scoped `ICurrentUserContext` into the singleton exception handler failed DI scope validation at startup — fixed by reading the `sub` claim directly from `HttpContext.User` instead.)

## Phase 10 — API Layer

- [x] `QuotationsController` — 5 thin endpoints (`pipeline`, `conversion`, `aging`, `{id}`, `summary`)
- [x] JWT bearer authentication configured in `Program.cs` — dev-only symmetric signing key until the Identity service exists (security-plan.md §6 open dependency)
- [x] Global exception-handling middleware → RFC 7807 Problem Details responses (`GlobalExceptionHandler`, `traceId` attached via `CustomizeProblemDetails`)
- [x] Response shape matches `docs/plans/api-contract.md` exactly (including `lastRefresh` on every response) — verified via 10 end-to-end tests (`SalesDeliveryBI.Api.Tests`) hitting the real HTTP pipeline against the seeded dev DB/Redis: 401 (no token), 403 (missing permission), 403 (unit outside assignment, with `traceId`), 404 (unknown quotation), 200 for all 5 endpoints with the `{data, lastRefresh}` envelope
- [x] Swagger/OpenAPI spec generated and matches the contract doc — fetched `/openapi/v1.json` from the running app, confirmed all 5 routes/params/response schemas

## Phase 11 — Testing

- [x] `Application.Tests` — `QuotationAppService` methods with mocked repository/cache/guard, including `IUnitAccessGuard` denial paths — `QuotationAppServiceTests` (10 tests, hand-rolled fakes, no mocking library needed for 3 small interfaces): guard-before-cache-before-repository ordering, correct cache key/TTL per dashboard, resolved scope reaches the repository unchanged, and `ForbiddenAccessException` from the guard propagates without ever touching cache or repository, for all 5 methods. "Caller missing `bi.quotation.view` entirely" is enforced by the `QuotationRead` authorization policy before the controller action runs, not by `IUnitAccessGuard` — covered instead by `GetPipeline_MissingQuotationViewPermission_Returns403` in `Api.Tests`.
- [x] `Infrastructure.Tests` — Dapper queries + EF Core migrations against a real test Postgres — `QuotationRepositoryTests` (Phase 5, 6 tests) against the seeded dev Postgres; migrations already verified end-to-end in Phase 2.
- [x] Integration test: full request → `403` when `unitId` outside caller's `user_units` and caller lacks `bi.quotation.viewAllUnits` — `GetPipeline_RequestedUnitOutsideAssignment_Returns403ProblemDetailsWithTraceId` (Phase 10)
- [x] Integration test: caller with `bi.quotation.viewAllUnits` can query any unit, including `unitId=null` for a global view — `GetPipeline_ViewAllUnits_Returns200WithDataAndLastRefreshEnvelope` (null) + `GetPipeline_ViewAllUnits_CanQueryAnyUnitEvenWithNoAssignment` (specific unrelated unit, zero assignments)
- [x] Integration test: cache hit vs. miss path returns identical payload — `RedisCacheServiceTests.GetOrSetAsync_CacheMiss_CallsFactoryOnceThenServesFromCache` (Phase 6): factory computes once on miss, the hit path returns the identical value without recomputing

## Phase 12 — Hardening / Verification

- [ ] Load test: concurrent MV refresh + concurrent dashboard reads (confirm `CONCURRENTLY` prevents lock contention)
- [ ] Load test: cache stampede scenario — simulate many concurrent requests on a cold cache key
- [ ] Security review: attempt row-level bypass on every endpoint's `unitId` param
- [ ] Confirm FX/currency values are correct against a manually verified sample (Finance sign-off)
- [ ] Confirm "Data as of {lastRefresh}" renders correctly end-to-end (backend value reaches frontend unmodified)
