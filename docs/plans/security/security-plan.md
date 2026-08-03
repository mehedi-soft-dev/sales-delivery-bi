# SalesDeliveryBI — Security Plan

**Principle:** access control is enforced server-side, in the repository layer, on every request. The frontend never decides what a user can see — it only reflects what the API already filtered.

---

## 1. JWT Claim Structure

Token issuance (login/auth) is a separate concern from this BI module — but since nothing exists yet, note that `sub` and `user_units` must resolve to the **same GUIDs** used in the `sales.Users` / `sales.UserUnits` tables designed in `database/schema-plan.md`, so `CreatedBy`/`ModifiedBy` audit columns and the row-level unit filter both trace back to the same identity.

```json
{
  "sub": "7b2e1a4c-1234-4a5b-8c9d-0e1f2a3b4c5d",
  "name": "Mehedi Hasan",
  "role": "CommercialOfficer",
  "user_units": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "9c4d2e11-8a2b-4f3e-9a1a-5b6c7d8e9f01"],
  "exp": 1754236800
}
```

- `role` — single role name, drives policy-based authorization.
- `user_units` — array of unit IDs the user is assigned to, sourced from `sales.user_units` at token issuance. Drives row-level filtering.

**Dependency:** if the identity service doesn't currently embed `user_units` in the token, this must be added there first — this BI module cannot compute it independently without re-querying OLTP on every request (defeats the caching strategy).

---

## 2. Role → Access Matrix

| Role | Quotation Access |
|---|---|
| SuperAdmin | Full, all units |
| GeneralManager | Full view + conversion analysis, all units |
| CommercialManager | Create/edit + all reports, assigned units |
| CommercialOfficer | Create/edit own + view team pipeline, assigned units |
| Merchandiser | Own quotations + limited pipeline, assigned units |
| FinanceManager | View only (value & conversion), all units |
| Viewer | Read-only summary, assigned units |

Since this module is **read-only reporting**, "create/edit" in the matrix above applies to the OLTP entry module, not this API — here, every role above maps to one of two effective levels:

- **All-units read** — SuperAdmin, GeneralManager, FinanceManager
- **Assigned-units-only read** — CommercialManager, CommercialOfficer, Merchandiser, Viewer

---

## 3. ASP.NET Core Policy Mapping

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AllUnitsRead", policy =>
        policy.RequireRole("SuperAdmin", "GeneralManager", "FinanceManager"));

    options.AddPolicy("AssignedUnitsRead", policy =>
        policy.RequireRole("SuperAdmin", "GeneralManager", "FinanceManager",
                            "CommercialManager", "CommercialOfficer", "Merchandiser", "Viewer"));
});
```

All 5 quotation endpoints require at minimum `AssignedUnitsRead` — the distinction between "all units" and "assigned units only" is enforced by the row-level filter (§4), not by a separate policy, since every role can call the same endpoint with a narrower or wider `unitId` scope.

---

## 4. Row-Level Security (enforced in Application/Infrastructure layer, per `architecture.md`)

`UnitSecurityBehavior` (MediatR pipeline behavior) runs before every query handler:

1. Read `user_units` claim from `ICurrentUserContext`.
2. If caller's role is in the "all units" set (§3) → no restriction, `unitId` param passed through as-is (including `null` = all).
3. Otherwise → if request's `unitId` is `null`, replace with caller's full `user_units` list; if request's `unitId` is set, verify it's a member of `user_units` — if not, throw `ForbiddenAccessException` → mapped to HTTP `403`.

**Never** return a silently-empty result for an out-of-scope unit — a `403` makes the boundary visible; an empty grid looks like a bug during support/debugging.

---

## 5. What Is Explicitly Out of Scope Here

- Authentication (login, token issuance, refresh) — owned by the ERP's existing identity service.
- Write-side authorization (who can create/edit a quotation) — owned by the OLTP transactional module.
- This module only authorizes **read access to BI/reporting data**.
