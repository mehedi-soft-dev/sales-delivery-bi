# CLAUDE.md — Sales Quotation BI Module

Project-specific rules. These apply on top of the global engineering standard (senior-level, no shortcuts). Follow exactly — do not deviate without discussing first. Full detail lives in `docs/plans/`.

---

## 1. Architecture — Strict Layering (Clean Architecture, CQRS-lite)

```
Controller → MediatR Query/Handler → Repository → Dapper (bi.* reads) / EF Core (sales schema + migrations)
```

- Controllers **never** touch `DbContext`, raw SQL, or Dapper directly. Controller only: extracts JWT claims, sends a MediatR query, returns the result.
- Every dashboard endpoint is a **Query** (no Commands in this module — no create/edit AppServices).
- `UnitSecurityBehavior` and `CachingBehavior` (MediatR pipeline behaviors) run centrally for every query — a handler cannot forget to apply row-level security or caching.
- Repositories are the only layer allowed to touch the database. All `bi.*` materialized-view reads go through Dapper — no EF change-tracking on read-only aggregates.
- Row-level security (`user_units` filter) is enforced in `UnitSecurityBehavior`/repository layer, never in the controller, never trusted from the client. A user requesting a unit outside their assignment gets `403`, never a silently-empty result.
- No bypass path: every query touching `bi.*` views applies the unit filter — no "admin shortcut" that skips it, even internally.
- See `docs/plans/backend/architecture.md` for the full solution structure and dependency rule.

## 2. Database

- Two schemas, **both created by this project** (nothing pre-exists): `sales` (OLTP, EF Core Code First) and `bi` (materialized views, raw SQL).
- **Every table has**: `Id` (GUID, app-generated — no int/serial PKs), `CreatedBy` (GUID), `CreatedDate`, `ModifiedBy` (GUID, nullable), `ModifiedDate` (nullable). Populated only via the `AuditableEntitySaveChangesInterceptor` — never set manually in code.
- Every materialized view must have a `CREATE UNIQUE INDEX` on its natural key — required for `REFRESH MATERIALIZED VIEW CONCURRENTLY`. No MV ships without one.
- Every refresh writes to `bi.mv_refresh_log`. Every dashboard API response includes `lastRefresh` sourced from this table.
- FX/currency conversion: rate is snapshotted at transaction date, never recomputed at refresh time.
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

## 4. Frontend — Angular 22 + AG Grid / PrimeNG

- **Always responsive.** Every dashboard, KPI card, grid, and chart must work correctly at mobile, tablet, and desktop breakpoints — no fixed-width layouts, no horizontal scroll on KPI card rows, grids scroll within their own container only.
- Use Angular's standalone components + signals; no legacy NgModule sprawl for new features.
- Verify AG Grid Angular wrapper + PrimeNG compatibility with Angular 22 before scaffolding — pin to the highest version both support if either lags.
- AG Grid / PrimeNG grid columns must have responsive column visibility (hide low-priority columns on narrow viewports) rather than shrinking illegibly.
- Every dashboard shows a "Data as of {lastRefresh}" indicator — non-negotiable, sourced from the API response, never hardcoded.
- No dashboard assumes real-time data. Loading and stale-data states must be handled explicitly (skeleton/spinner, not a blank screen).
- Full structure: `docs/plans/frontend/architecture.md`.

## 5. Security

- Role matrix (SuperAdmin, GeneralManager, CommercialManager, CommercialOfficer, Merchandiser, FinanceManager, Viewer) is enforced via policy-based authorization in ASP.NET Core — never via UI hiding alone. Hiding a button is not a security boundary.
- JWT carries `role`, `user_units` (GUID array), `sub` (caller's user GUID). Nothing about access control is inferred from the frontend.
- Full policy mapping: `docs/plans/security/security-plan.md`.

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
- Don't build Sales Order / Delivery / Invoice / Return dashboards unless explicitly scoped in — current phase is Sale Quotations only (Pipeline, Conversion/Win-Loss, Aging).
