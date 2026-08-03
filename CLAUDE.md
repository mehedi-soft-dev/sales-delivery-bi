# CLAUDE.md — Sales Quotation BI Module

Project-specific rules. These apply on top of the global engineering standard (senior-level, no shortcuts). Follow exactly — do not deviate without discussing first. Full detail lives in `docs/plans/`.

**Priority order: access control and caching are the most important parts of this system.** Row-level unit security (`IUnitAccessGuard`, §1/§5) and Redis cache-aside + stampede protection (§3) are non-negotiable on every endpoint — get these right and keep them right before polishing dashboards or UI. Any change that touches a `QuotationAppService` method must not weaken either without discussing first.

---

## 1. Architecture — Strict Layering (Clean Architecture, plain AppServices — no MediatR/CQRS)

```
Controller → QuotationAppService → Repository → Dapper (bi.* reads) / EF Core (sales schema + migrations)
```

- **No MediatR, no CQRS.** Demo project, single developer, 5 read-only endpoints — dropped as overkill. Plain `QuotationAppService` class with one method per dashboard.
- Controllers **never** touch `DbContext`, raw SQL, or Dapper directly. Controller only: calls the AppService method, returns the result.
- Every dashboard endpoint is a **read-only query method** (no Commands, no create/edit AppServices in this module).
- Each `QuotationAppService` method **explicitly** calls `IUnitAccessGuard.Validate(unitId)` then `ICacheService.GetOrSetAsync(...)` — same responsibilities the old MediatR behaviors had, just as plain method calls. **No pipeline enforces this automatically** — every new method must remember to call both, by convention, not by structure.
- Repositories are the only layer allowed to touch the database. All `bi.*` materialized-view reads go through Dapper — no EF change-tracking on read-only aggregates.
- Row-level security (`user_units`/`permissions` claims) is enforced in `IUnitAccessGuard`, never in the controller, never trusted from the client. A user requesting a unit outside their assignment gets `403`, never a silently-empty result.
- No bypass path by convention: every AppService method touching `bi.*` views must call the guard — treat skipping it as a bug, since nothing else catches it.
- See `docs/plans/backend/architecture.md` for the full solution structure and dependency rule.

## 2. Database

- Two schemas, **both created by this project** (nothing pre-exists): `sales` (OLTP, EF Core Code First) and `bi` (materialized views, raw SQL).
- **Every table has**: `Id` (GUID, app-generated — no int/serial PKs), `CreatedBy` (GUID), `CreatedDate`, `ModifiedBy` (GUID, nullable), `ModifiedDate` (nullable). Populated only via the `AuditableEntitySaveChangesInterceptor` — never set manually in code.
- Every materialized view must have a `CREATE UNIQUE INDEX` on its natural key — required for `REFRESH MATERIALIZED VIEW CONCURRENTLY`. No MV ships without one.
- Every refresh writes to `bi.mv_refresh_log`. Every dashboard API response includes `lastRefresh` sourced from this table.
- FX/currency conversion: rate is snapshotted at transaction date, never recomputed at refresh time.
- MV refresh scheduling = `pg_cron` (inside Postgres). Cache warm-up after each refresh = **Quartz.NET** (`Quartz`, `Quartz.Extensions.Hosting`), triggered on an offset schedule — see `docs/plans/backend/architecture.md` §Cache Warm-up Job.
- Single migration pipeline: EF Core (`dotnet ef database update`) creates both schemas — `sales` tables from Code First entities, `bi` schema/MVs/pg_cron via a hand-written `migrationBuilder.Sql(...)` migration. No separate Flyway/DbUp tool.
- Dev/test seed data (GUID-mapped from the 30-row guideline sample) lives in `docs/plans/database/seed-data.md` — loaded via a `DatabaseSeeder`, guarded to `IsDevelopment()` only, never in Production.

## 3. API Rules

- Every endpoint accepts `unitId` (nullable GUID), `fromDate`, `toDate`. All IDs in requests/responses are GUIDs — no integer IDs anywhere.
- Server re-validates `unitId` against the caller's JWT `user_units` claim on every request — never trust a client-supplied unit list.
- Every response includes `lastRefresh`.
- Errors use RFC 7807 Problem Details (`type, title, status, detail, traceId`).
- Cache-aside via Redis; cache key = dashboard name + hash of all filter params.
- Cache stampede protection required on every cached endpoint: short-lived Redis lock (`SET key NX PX 2000`) around recompute — never let a TTL expiry cause a thundering herd on Postgres.
- Full request/response shape per endpoint: `docs/plans/api-contract.md`.

## 4. Frontend — Angular 22 + AG Grid / PrimeNG / ApexCharts

- **Always responsive.** Every dashboard, KPI card, grid, and chart must work correctly at mobile, tablet, and desktop breakpoints — no fixed-width layouts, no horizontal scroll on KPI card rows, grids scroll within their own container only.
- Use Angular's standalone components + signals; no legacy NgModule sprawl for new features.
- Charts use **ApexCharts** via the `ng-apexcharts` wrapper (conversion trend line, win/loss bar, aging buckets bar) — grids stay AG Grid, cards/inputs stay PrimeNG.
- Verify AG Grid Angular wrapper + PrimeNG + `ng-apexcharts` compatibility with Angular 22 before scaffolding — pin to the highest version all three support if one lags.
- AG Grid / PrimeNG grid columns must have responsive column visibility (hide low-priority columns on narrow viewports) rather than shrinking illegibly.
- Every dashboard shows a "Data as of {lastRefresh}" indicator — non-negotiable, sourced from the API response, never hardcoded. This is the frontend's visible proof that caching is working correctly — treat a wrong/stale value here as a caching bug, not a cosmetic one.
- A `403` response is handled globally as a distinct "not authorized" state, never folded into a generic error page — access control must stay visible to the user, not silently swallowed.
- No dashboard assumes real-time data. Loading and stale-data states must be handled explicitly (skeleton/spinner, not a blank screen).
- Full structure: `docs/plans/frontend/architecture.md`.

## 5. Security

- **Dynamic RBAC, owned by a separate Identity service** — this repo never creates/edits users, roles, or permissions. It only validates JWTs and checks permission claims.
- Authorization is **permission-based, not role-name-based** (`RequireClaim("permission", "bi.quotation.view")`) — roles are admin-defined elsewhere, this API only knows permission codes.
- JWT carries `sub` (caller's user GUID), `permissions` (array of permission codes), `user_units` (GUID array). Nothing about access control is inferred from the frontend.
- Full policy mapping + permission codes: `docs/plans/security/security-plan.md`.

## 6. Logging — Serilog (file sink)

- Structured logging only — always `_logger.LogInformation("... {UnitId}", unitId)`, never string interpolation.
- Rolling file sink (`logs/salesdeliverybi-.log`, daily rolling, 30-day retention) + `UseSerilogRequestLogging()` for per-request logs.
- Never log PII or full JWTs — log `sub`/`unitId`, not names/emails.
- Full config: `docs/plans/backend/architecture.md` §Logging.

## 7. Code Quality Non-Negotiables

- No commented-out code, no dead code.
- No magic strings — use constants/enums (status values, role names, cache key prefixes).
- Async all the way — never `.Result` / `.Wait()`.
- Null safety — never assume a reference is non-null without checking.
- DTOs ≠ Entities — handlers never return raw entities, only DTOs.
- A method over ~30 lines is a signal to split it.
- No TODOs, no placeholders, no "add this later" — every PR ships complete for its stated scope.

## 8. Scope Discipline

- This module is **read-only BI/reporting**. The `sales` schema is designed and migrated here (nothing pre-exists), but **no create/edit AppServices or entry UI are built** — data entry stays a separate future concern; dev data comes from the seed script only.
- **User/Role/Permission management is explicitly out of scope here** — that's the separate Identity service/repo. Never add User/Role/Permission CRUD, login, or token-issuance endpoints to this solution.
- Don't build Sales Order / Delivery / Invoice / Return dashboards unless explicitly scoped in — current phase is Sale Quotations only (Pipeline, Conversion/Win-Loss, Aging).
