# SalesDeliveryBI — Security Plan

**Principle:** access control is enforced server-side, in the repository layer, on every request. The frontend never decides what a user can see — it only reflects what the API already filtered.

**Decision (original):** User/Role/Permission management was originally planned as a dynamic RBAC system owned by a separate Identity service/repo, with SalesDeliveryBI as a pure JWT consumer that never creates/edits users, roles, or permissions.

**Revised (discussed with the user, see §5/§6 below):** that separate service was never built, and the user asked for a real, working login flow against this app's own Angular frontend — hand-minted test JWTs weren't sufficient. `User`/`Role`/`UserUnit`/`RolePermission` are now real EF Core entities in `SalesDeliveryBI.Domain`/`sales` schema, and `POST /api/auth/login` issues real JWTs from this process. Roles are real seeded table rows (`sales.Roles`, matching the 7 names in `docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md` §5), and role→permission mapping is a real seeded table too (`sales.RolePermissions`, resolving a role to the 2 permission codes this API actually checks) — not an in-code static mapping. This is a scope-contained version of "the Identity service" — no admin CRUD UI for users/roles/permissions was added (§5 below), only login + the entities/seed data needed to make it real.

---

## 1. Why Dynamic RBAC Changes the Claim Shape

With fixed roles, a single `role` string claim was enough (`RequireRole("SuperAdmin", ...)`). With **dynamic RBAC** — admins can create custom roles and attach arbitrary permissions — authorization must be **permission-based**, not role-name-based: SalesDeliveryBI has no idea what roles exist at any given time, only which **permission codes** the token carries.

**Permission naming convention:** `{module}.{resource}.{action}` — e.g. `bi.quotation.view`, `bi.quotation.viewAllUnits`.

| Permission Code | Grants |
|---|---|
| `bi.quotation.view` | Access to the 5 quotation dashboard endpoints, scoped to the caller's assigned units |
| `bi.quotation.viewAllUnits` | Removes the unit restriction — caller can query any unit, or omit `unitId` for a global view |

The Identity service decides which permissions go on which role, and which roles a user has — SalesDeliveryBI just reads the resulting flat permission list off the token.

---

## 2. JWT Claim Structure (contract expected from the Identity service)

```json
{
  "sub": "7b2e1a4c-1234-4a5b-8c9d-0e1f2a3b4c5d",
  "name": "Mehedi Hasan",
  "permissions": ["bi.quotation.view"],
  "user_units": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "9c4d2e11-8a2b-4f3e-9a1a-5b6c7d8e9f01"],
  "exp": 1754236800
}
```

- `sub` — caller's user GUID. Must match the `Id` used in `sales.Quotations.CreatedBy`/`ModifiedBy` if this same identity created audit rows (relevant mainly for the seed data and future write-side modules, not this read-only API).
- `permissions` — flat array of permission codes resolved by the Identity service from the user's role(s) at token-issuance time (not resolved per-request here — keeps this API stateless and cache-friendly).
- `user_units` — array of unit GUIDs the user is assigned to. Drives row-level filtering (§4).

**Dependency on the Identity service:** it must resolve a user's role(s) into a flat `permissions` array and embed `user_units` at token issuance — this BI module cannot compute either without re-querying another service on every request, which would defeat the Redis caching strategy entirely.

---

## 3. ASP.NET Core Policy Mapping

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("QuotationRead", policy =>
        policy.RequireClaim("permission", "bi.quotation.view"));

    options.AddPolicy("QuotationReadAllUnits", policy =>
        policy.RequireClaim("permission", "bi.quotation.viewAllUnits"));
});
```

All 5 quotation endpoints require `QuotationRead`. The distinction between "all units" and "assigned units only" is resolved inside `UnitSecurityBehavior` (§4) by checking for the `bi.quotation.viewAllUnits` claim — not by a second `[Authorize]` policy on the endpoint — since the same endpoint serves both cases depending on what the caller's token grants.

---

## 4. Row-Level Security (enforced in Application/Infrastructure layer, per `backend/architecture.md`)

`IUnitAccessGuard.Validate(unitId)` — called explicitly at the top of every `QuotationAppService` method (no MediatR pipeline in this project; see `backend/architecture.md` for why):

1. Read `permissions` and `user_units` claims from `ICurrentUserContext`.
2. If `bi.quotation.viewAllUnits` is present → no restriction, `unitId` param passed through as-is (including `null` = all).
3. Otherwise → if request's `unitId` is `null`, replace with caller's full `user_units` list; if request's `unitId` is set, verify it's a member of `user_units` — if not, throw `ForbiddenAccessException` → mapped to HTTP `403`.

**Never** return a silently-empty result for an out-of-scope unit — a `403` makes the boundary visible; an empty grid looks like a bug during support/debugging.

---

## 5. What Is Explicitly Out of Scope Here

- **User/Role/Permission admin CRUD** — no UI or endpoint to create/edit/delete users, roles, or role→permission mappings. The dev login users (one per seeded role), 7 seeded roles, and their `sales.RolePermissions` mapping rows come only from `DatabaseSeeder` (`IsDevelopment()`-guarded, same as the rest of the seed data) — there's no write path for any of it at runtime.
- **Token refresh** — `POST /api/auth/login` issues a single 8-hour token; no refresh-token flow exists. Re-login is the only way to get a new token once one expires.
- **Write-side authorization** (who can create/edit a quotation) — still owned by the (not-yet-built) OLTP transactional module, unrelated to login.
- This module authorizes **read access to BI/reporting data**, based on permission claims resolved from the caller's seeded `Role` at login time — never claims computed per-request.

---

## 6. Resolved: Real Login, Fixed-Enum-Style Roles (not dynamic RBAC)

The Identity-service dependency this section used to describe is resolved for this project: `Domain/Entities/User.cs`, `Role.cs`, `RolePermission.cs`, `UserUnit.cs` (+ `RoleNames` constants) live in `SalesDeliveryBI.Domain`; `AuthAppService` (Application), `PasswordHasher`/`JwtTokenGenerator`/`UserRepository` (Infrastructure/Security), and `AuthController` (`POST /api/auth/login`) implement the actual login flow.

**Revised (discussed with the user): role→permission mapping is a real DB table**, not the in-code static dictionary this section originally described. `sales.RolePermissions` (`RoleId`, `PermissionCode`, unique index on the pair) is seeded by `DatabaseSeeder` alongside the 7 roles — this API still only ever checks 2 permission codes (`bi.quotation.view`, `bi.quotation.viewAllUnits`), so the table stays a simple join, not a full admin-editable `Permission` catalog. `UserRepository.FindByEmailAsync` eager-loads `Role.RolePermissions` in the same query as `Role`/`UserUnits`, and `JwtTokenGenerator` reads permission codes off `user.Role.RolePermissions` directly — no separate lookup or in-code mapping table. Deliberately still **not** built: a full `Permission` catalog table or any admin CRUD over roles/permissions — `RolePermissions` rows only ever come from `DatabaseSeeder`.
