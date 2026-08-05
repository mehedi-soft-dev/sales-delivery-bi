# SalesDeliveryBI — Database Schema Plan (PostgreSQL, EF Core Code First)

**Scope:** nothing exists yet. This project designs and creates **both** schemas:
- **`sales`** — OLTP tables, Code First via EF Core (owned by this project since nothing pre-exists).
- **`bi`** — materialized views + refresh metadata, raw SQL (MVs aren't EF-mappable entities).

Even though the `sales` schema is created here, **no write/create/edit AppServices are built** against it in this module (per `CLAUDE.md` scope discipline) — the tables exist so the BI views have something to read. Data entry stays a future/separate concern; for now, rows are seeded directly (SQL/seed scripts) for development and testing.

---

## Entity/Audit Convention

Every `sales` table follows the same shape — see `backend/architecture.md` for the `BaseEntity` definition:

```
Id              uuid        PK, app-generated (Guid.NewGuid())
CreatedBy       uuid        NOT NULL
CreatedDate     timestamptz NOT NULL
ModifiedBy      uuid        NULL
ModifiedDate    timestamptz NULL
```

No `int`/`serial` identity columns anywhere. Populated automatically by `AuditableEntitySaveChangesInterceptor` — never set manually.

---

## `sales` Schema — Code First Entities

```csharp
public class Unit : BaseEntity
{
    public string UnitName { get; set; }
    public string UnitType { get; set; }   // Knit, Woven, Sweater...
}

public class Buyer : BaseEntity
{
    public string BuyerName { get; set; }
}

public class Merchandiser : BaseEntity
{
    public string MerchandiserName { get; set; }
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; }
}

public class User : BaseEntity
{
    public string UserName { get; set; }
    public UserRole Role { get; set; }      // enum: SuperAdmin, GeneralManager, CommercialManager, ...
}

public class UserUnit : BaseEntity          // row-level security join
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; }
}

public class FxRate : BaseEntity
{
    public string CurrencyCode { get; set; }
    public DateOnly RateDate { get; set; }
    public decimal RateToUsd { get; set; }
}

public class Quotation : BaseEntity
{
    public string QuotationNo { get; set; }
    public DateOnly QuotationDate { get; set; }
    public Guid BuyerId { get; set; }
    public Buyer Buyer { get; set; }
    public Guid MerchandiserId { get; set; }
    public Merchandiser Merchandiser { get; set; }
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; }
    public string StyleNo { get; set; }            // lead/summary style — line-level detail lives in QuotationItem
    public string Season { get; set; }
    public string CurrencyCode { get; set; }
    public decimal Value { get; set; }             // net total after Discount; Subtotal = Value + Discount, computed at read time
    public string Incoterm { get; set; }           // e.g. FOB
    public string PaymentTerm { get; set; }         // e.g. "30 Days"
    public DateOnly ValidUntil { get; set; }
    public decimal Discount { get; set; }
    public QuotationStatus Status { get; set; }   // enum: Draft, Submitted, Negotiation, PendingApproval, Approved, Rejected, Expired, Converted
    public DateTime StatusDate { get; set; }
    public string? ConvertedToSoNo { get; set; }   // reference only — Sales Order module owns the actual entity, out of scope here
    public DateTime? ConvertedDate { get; set; }
    public string? LostReason { get; set; }

    public ICollection<QuotationItem> Items { get; set; }
    public ICollection<QuotationStatusHistory> StatusHistory { get; set; }
}

public class QuotationItem : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation Quotation { get; set; }
    public string StyleNo { get; set; }
    public string ItemDescription { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class QuotationStatusHistory : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation Quotation { get; set; }
    public QuotationStatus Status { get; set; }
    public DateTime StatusDate { get; set; }
    public string? Note { get; set; }
}
```

`QuotationItem`/`QuotationStatusHistory` back the Quotation Details drill-in view (line items grid, status timeline) — they don't feed `bi.mv_sales_quotation_summary` or the two supporting MVs, which stay header-level for the Pipeline/Conversion/Aging dashboards. Detail-view queries join these tables directly (Dapper), not through the MVs.

**Fluent API configuration** (in `Infrastructure/Persistence/EfCore/EntityConfigurations/*Configuration.cs`, not on the entities themselves):
- `ToTable("quotations", "sales")` per entity, mapping to the `sales` schema explicitly.
- `HasIndex(q => q.QuotationNo).IsUnique()`.
- Enums stored as string (`HasConversion<string>()`) — never raw int — so the DB stays human-readable and safe to query directly during support.

---

## `bi` Schema — Materialized Views (raw SQL, GUID-aware)

### 1. `bi.mv_sales_quotation_summary` (primary)

```sql
CREATE MATERIALIZED VIEW bi.mv_sales_quotation_summary AS
SELECT
    q."Id" AS quotation_id, q."QuotationNo" AS quotation_no, q."QuotationDate" AS quotation_date,
    q."BuyerId" AS buyer_id, b."BuyerName" AS buyer_name,
    q."MerchandiserId" AS merchandiser_id, m."MerchandiserName" AS merchandiser_name,
    q."UnitId" AS unit_id, u."UnitName" AS unit_name,
    q."StyleNo" AS style_no, q."Season" AS season, q."CurrencyCode" AS currency_code,
    q."Value" * fx."RateToUsd" AS quotation_value_usd,
    q."Status" AS status, q."StatusDate" AS status_date,
    (CURRENT_DATE - q."StatusDate"::date) AS days_in_status,
    (COALESCE(q."ConvertedDate", now())::date - q."QuotationDate") AS days_open,
    q."ConvertedToSoNo" AS converted_to_so_no, q."ConvertedDate" AS converted_date,
    (q."ConvertedDate"::date - q."QuotationDate") AS conversion_days,
    q."LostReason" AS lost_reason, q."CreatedBy" AS created_by,
    now() AS last_refresh_date
FROM sales."Quotations" q
JOIN sales."Buyers" b ON b."Id" = q."BuyerId"
JOIN sales."Merchandisers" m ON m."Id" = q."MerchandiserId"
JOIN sales."Units" u ON u."Id" = q."UnitId"
LEFT JOIN sales."FxRates" fx ON fx."CurrencyCode" = q."CurrencyCode"
                              AND fx."RateDate" = q."QuotationDate";

CREATE UNIQUE INDEX ux_mv_quotation_summary ON bi.mv_sales_quotation_summary (quotation_id);
```

`quotation_id`, `buyer_id`, `merchandiser_id`, `unit_id` are all `uuid` — matches the API contract's GUID-typed IDs.

### 2. `bi.mv_quotation_pipeline_daily` — dropped (discussed with the user)

Originally a "daily snapshot" grouped by `status`, `unit_id`. Dropped via the `DropQuotationPipelineDailyMv` migration: nothing ever queried it, and as a materialized *view* (fully recomputed and replaced on every `REFRESH`, `snapshot_date` always `CURRENT_DATE`) it had no append/history mechanism — it could never accumulate a multi-day time series, which was the entire point of a "daily snapshot." A real version of this feature would need an append-only snapshot table, not a materialized view. The Pipeline/Aging dashboards get their current-state numbers directly from `mv_sales_quotation_summary` and never needed this MV.

### 3. `bi.mv_quotation_conversion_rate` (supporting)

Monthly rollup, grouped by `buyer_id`, `merchandiser_id`, `unit_id`, `date_trunc('month', quotation_date)`.

---

## Refresh Metadata

```sql
CREATE TABLE bi.mv_refresh_log (
    id BIGSERIAL PRIMARY KEY,   -- internal-only log table, not domain data — serial is fine here
    mv_name TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    finished_at TIMESTAMPTZ,
    status TEXT NOT NULL DEFAULT 'RUNNING',
    rows_affected BIGINT,
    error_message TEXT
);
```

---

## Refresh Cadence (`pg_cron`, `REFRESH MATERIALIZED VIEW CONCURRENTLY`)

| MV | Cadence |
|---|---|
| `mv_sales_quotation_summary` | 3 min |
| `mv_quotation_conversion_rate` | 15 min |

`CONCURRENTLY` requires the unique index above — without it, refresh takes an `ACCESS EXCLUSIVE` lock and dashboards hang mid-refresh. No MV ships without its unique index.

---

## Migration Tooling — Single Pipeline via EF Core

Since this project now owns both schemas, use **one** migration mechanism instead of two tools:

1. `dotnet ef migrations add InitialCreate` — generates `sales` schema tables from the Code First entities above.
2. A follow-up EF Core migration (`AddBiSchemaAndMaterializedViews`) that uses `migrationBuilder.Sql(...)` to run the raw SQL for: `CREATE SCHEMA bi`, the 3 materialized views + unique indexes, `bi.mv_refresh_log`, and the `pg_cron` schedule statements.

```
Migrations/
├── 20260803_InitialCreate.cs          (EF Core generated — sales schema tables)
└── 20260803_AddBiSchemaAndViews.cs    (hand-written migrationBuilder.Sql — bi schema + MVs + pg_cron)
```

This keeps `dotnet ef database update` as the single command that stands up the entire database from scratch — no separate Flyway/DbUp step.

---

## Seed Data (development/testing only)

Since there's no entry UI in scope, seed a small dataset directly via an EF Core seed migration or a `dotnet run --seed` console command — matching the 30-row sample dataset already in `docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md` §7, translated to GUID keys.

---

## Open Dependency — FX Conversion

`quotation_value_usd` depends on `FxRate` rows existing at quotation-date granularity. Confirm rate source/ownership with Finance before Phase 1 sign-off, and decide who is responsible for keeping `sales."FxRates"` populated going forward (this module only reads it).
