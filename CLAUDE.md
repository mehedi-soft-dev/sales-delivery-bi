# CLAUDE.md — Sales Quotation BI Module

Project-specific rules. These apply on top of the global engineering standard (senior-level, no shortcuts). Follow exactly — do not deviate without discussing first. Full detail lives in `docs/plans/`.

**Priority order: access control and caching are the most important parts of this system.** Row-level unit security (`IUnitAccessGuard`, §1/§5) and Redis cache-aside + stampede protection (§3) are non-negotiable on every endpoint — get these right and keep them right before polishing dashboards or UI. Any change that touches a `QuotationAppService` method must not weaken either without discussing first.

---

## 1. Architecture — Strict Layering (Clean Architecture, plain AppServices — no MediatR/CQRS)

```
Controller → QuotationAppService → Repository → Dapper (bi.* reads) / EF Core (sales schema + migrations)
```

- **No MediatR, no CQRS.** Demo project, single developer, a handful of read-only endpoints — dropped as overkill. Plain `QuotationAppService`/`AdminAppService` classes, one method per dashboard/view.
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
- Cache-aside via Redis; cache key = dashboard name + hash of all filter params (unit/date scope only — **never** page/sort, see below).
- Cache stampede protection required on every cached endpoint: short-lived Redis lock (`SET key NX PX 2000`) around recompute — never let a TTL expiry cause a thundering herd on Postgres.
- **Revised (discussed with the user): grid rows are server-side paged/sorted**, via `page`, `pageSize`, `sortField`, `sortDescending` query params (`Application/Common/GridQuery.cs`), bound as one `[FromQuery] GridQuery` per grid endpoint. The row-level response shape is `PagedResult<T>` (`Items`, `TotalCount`, `Page`, `PageSize`). Critically, **paging/sorting is applied in-memory in the AppService, AFTER the cached fetch** — the repository still returns the FULL unpaged row list, cached under the exact same unit+date-scoped key as before, so `CacheWarmupJob`'s fixed warm set stays valid for every page/sort a client requests. Never add page/sort to a cache key — that would multiply cache entries per page and break warm-up.
- `sortField` is validated against a per-grid allow-list of known property names (`GridPaging.Apply`'s `sortSelectors` dictionary) — plain LINQ `OrderBy`, never raw SQL, so there's no injection surface; an unrecognized `sortField` is a silent no-op, not an error.
- Full request/response shape per endpoint: `docs/plans/api-contract.md`.

## 4. Frontend — Angular 21 + PrimeNG (fully) + ApexCharts

**Pinned to Angular 21, not 22.** PrimeNG 22.x introduced a paid license-key requirement (`@primeui/license-manager`) with no Angular-22-compatible version that predates it — the last pre-license PrimeNG (21.1.9) only supports Angular 21. Discussed with the user; downgraded the whole stack (Angular 21.2.19, PrimeNG 21.1.9, `@primeuix/themes` 2.0.3, `@primeicons/angular` 8.0.0 as an explicit direct dependency since PrimeNG 21 no longer pulls it in transitively) rather than pay for a license or drop PrimeNG.

- **Revised (discussed with the user): grids are PrimeNG `p-table`, not AG Grid.** AG Grid and its dependencies (`ag-grid-angular`, `ag-grid-community`) are removed. Every grid (Pipeline, Conversion's buyer performance, Aging, and the new Admin views) uses `p-table` in **lazy/server-side mode** (`[lazy]="true"`, `(onLazyLoad)`) — the table never paginates or sorts client-side over a locally-held full dataset; every page turn and column sort is a real HTTP request carrying `page`/`pageSize`/`sortField`/`sortDescending`, answered by the API's `PagedResult<T>` (§3). `totalRecords` comes from `PagedResult.TotalCount`, never computed client-side.
- **Always responsive.** Every dashboard, KPI card, grid, and chart must work correctly at mobile, tablet, and desktop breakpoints — no fixed-width layouts, no horizontal scroll on KPI card rows, grids scroll within their own container only.
- Use Angular's standalone components + signals; no legacy NgModule sprawl for new features.
- Charts use **ApexCharts** via the `ng-apexcharts` wrapper (conversion trend line, win/loss bar, aging buckets bar) — grids and every other interactive control stay PrimeNG.
- `p-table` columns must have responsive column visibility (hide low-priority columns on narrow viewports) rather than shrinking illegibly.
- Every dashboard shows a "Data as of {lastRefresh}" indicator — non-negotiable, sourced from the API response, never hardcoded. This is the frontend's visible proof that caching is working correctly — treat a wrong/stale value here as a caching bug, not a cosmetic one.
- A `403` response is handled globally as a distinct "not authorized" state, never folded into a generic error page — access control must stay visible to the user, not silently swallowed.
- No dashboard assumes real-time data. Loading and stale-data states must be handled explicitly (skeleton/spinner, not a blank screen).
- Full structure: `docs/plans/frontend/architecture.md`.

## 5. Security

- **Revised (discussed with the user): real login lives here now**, superseding the original "separate Identity service" plan. `sales.Users`/`sales.Roles`/`sales.UserUnits` are real EF Core entities/tables in this repo; `POST /api/auth/login` (email+password) issues real JWTs via `IJwtTokenGenerator`, signed with the same `Jwt:SigningKey` the API validates against. Roles are real seeded table rows (`Role.Name`, 7 named roles per `docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md` §5). **Revised again (discussed with the user): role→permission mapping now lives in the DB too** — `sales.RolePermissions` (`RoleId`, `PermissionCode`, unique per pair) is a real EF Core entity/table, seeded by `DatabaseSeeder` alongside the 7 roles; the old in-code `UserRolePermissions` static dictionary is gone. `IUserRepository.FindByEmailAsync` eager-loads `Role.RolePermissions` in the same round trip as `Role`/`UserUnits`, and `JwtTokenGenerator` reads permission codes straight off that loaded collection. See `docs/plans/security/security-plan.md` §5/§6 for the full reasoning and what's still explicitly out of scope (write-side auth, admin CRUD UI for roles/users/permissions).
- Authorization is **permission-based, not role-name-based** (`RequireClaim("permission", "bi.quotation.view")`) at the API layer — the JWT only ever carries permission codes, never a role name; `sales.RolePermissions` is what translates a seeded role into those codes at token-issuance time.
- JWT carries `sub` (caller's user GUID), `name` (display name), `role` (display-only role name, added for the topbar's "role" subtitle — **never** consulted for access-control, that's `permissions`), `permissions` (array of permission codes), `user_units` (GUID array). Nothing about access control is inferred from the frontend.
- **Revised (discussed with the user): Admin > Users/Roles/Permissions is now in scope, view-only.** New `AdminController`/`AdminAppService`/`IAdminRepository` (EF Core reads over `sales.Users`/`Roles`/`RolePermissions`/`UserUnits`, uncached — no scheduled refresh job for OLTP rows, so no `lastRefresh`/`ICacheService`/`IUnitAccessGuard` involved, those are BI-dashboard-specific). Gated by a new `AuthorizationPolicies.AdminRead` policy requiring the `admin.access.view` permission code, seeded to `SuperAdmin` only (`DatabaseSeeder`). Still explicitly out of scope: see §8.
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

- This module is **read-only BI/reporting**. The `sales` schema is designed and migrated here (nothing pre-exists), but **no create/edit AppServices or entry UI are built** for the quotation data itself — data entry stays a separate future concern; dev data comes from the seed script only.
- **Auth login is now in scope, and Admin > Users/Roles/Permissions is now in scope as a read-only view (§5).** `User`/`Role`/`UserUnit`/`RolePermission` tables + `POST /api/auth/login` exist here — a deliberate, discussed reversal of the earlier "separate Identity service" plan, driven by the need for a real working login flow. **Still explicitly out of scope: any UI or endpoint to create/edit/delete users, roles, or role-permission mappings.** The new Admin pages only ever call the 3 `GET` endpoints (`/api/admin/users`, `/roles`, `/permissions`) — the 7 seed users, one per seeded role (`DatabaseSeeder`), remain the only way accounts get created, `IsDevelopment()`-guarded like the rest of the seed data.
- Don't build Sales Order / Delivery / Invoice / Return dashboards unless explicitly scoped in — current phase is Sale Quotations only (Pipeline, Conversion/Win-Loss, Aging).
- **Revised (discussed with the user): the sidebar nav now has menu-only placeholders for future modules** — Dashboard > Sales Orders / Delivery / Challan / Sales Invoice / Return / Credit Note, plus a top-level Report entry (`layout/nav-items.ts`'s `NAV_TREE`, `placeholder: true` on the leaf). Each routes to the shared `ComingSoonPage` (`features/coming-soon/`) — no backend endpoint, no real page behind them. Don't mistake these for dead code or half-finished features; they're intentional nav-first stubs the user asked for ("just menu, nothing else for now"). Building a real page for one of these still requires discussing scope first, same as any other module in this list.
