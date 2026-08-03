# SalesDeliveryBI — Seed Data

Source: 30-row sample in `docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md` §7, translated to the GUID-keyed Code First schema in `database/schema-plan.md`. Development/testing only — never runs in Production.

---

## Reference/Master Data

**System seed user** (used as `CreatedBy` for every seeded row):

| Id | UserName | Role |
|---|---|---|
| `44444444-4444-4444-4444-444444444401` | `seed.system` | SuperAdmin |

**Units:**

| Id | UnitName | UnitType |
|---|---|---|
| `11111111-1111-1111-1111-111111111101` | Unit-1 | Knit |
| `11111111-1111-1111-1111-111111111102` | Unit-2 | Woven |
| `11111111-1111-1111-1111-111111111103` | Unit-3 | Sweater |

**Buyers:**

| Id | BuyerName |
|---|---|
| `22222222-2222-2222-2222-222222222201` | H&M |
| `22222222-2222-2222-2222-222222222202` | Zara |
| `22222222-2222-2222-2222-222222222203` | Primark |
| `22222222-2222-2222-2222-222222222204` | C&A |
| `22222222-2222-2222-2222-222222222205` | Mango |
| `22222222-2222-2222-2222-222222222206` | Next |

**Merchandisers** (home unit = most frequent unit in the source sample; a merchandiser's quotations can still reference a different unit — `Quotation.UnitId` is independent, matching the source data):

| Id | MerchandiserName | UnitId (home) |
|---|---|---|
| `33333333-3333-3333-3333-333333333301` | Fatema Begum | Unit-1 |
| `33333333-3333-3333-3333-333333333302` | Jahid Hasan | Unit-2 |
| `33333333-3333-3333-3333-333333333303` | Mehedi Hasan | Unit-1 |
| `33333333-3333-3333-3333-333333333304` | Sumaiya Akter | Unit-2 |

**FX Rates:** every source value is already in USD, so seed `FxRate(CurrencyCode: "USD", RateToUsd: 1.0000)` — but the MV joins on `RateDate = QuotationDate` exactly, so a rate row is needed **per distinct quotation date** used below (27 distinct dates from 2026-06-12 to 2026-08-01). Don't hand-enumerate these in code — generate them in the seeder:

```csharp
var distinctDates = quotations.Select(q => q.QuotationDate).Distinct();
foreach (var date in distinctDates)
    context.FxRates.Add(new FxRate { CurrencyCode = "USD", RateDate = date, RateToUsd = 1.0000m });
```

---

## Quotations (30 rows)

`StatusDate` and `ConvertedDate` are seed approximations, not reproduced from the source (which only listed `DAYS_OPEN`, a derived value): for **Converted** rows, `ConvertedDate = QuotationDate + DaysOpen`, and `StatusDate = ConvertedDate`; for all other rows, `StatusDate = QuotationDate` (good enough for dev/testing — the MV computes `days_open`/`days_in_status` live from these dates regardless of when the query runs).

`LostReason` is a placeholder for **Rejected**/**Expired** rows — the source sample didn't include reasons.

**Fields added after the mockup review** (not in the original source sample, so no hand-authored per-row values — kept uniform/derived instead of invented):
- `Incoterm` = `"FOB"` for every row (most common garment-export term; vary manually in dev if you need to test other values).
- `PaymentTerm` = `"30 Days"` for every row.
- `Discount` = `0` for every row (subtotal == total for all seed rows).
- `ValidUntil` = `QuotationDate + 30 days` — computed in the seeder, not stored in `seed-quotations.json`.
- `StyleNo`/`Season` — the source sample never had concrete values (only listed as MV columns), so seed values are invented here for realism: `StyleNo` = `STY-40{01..30}` matching row number, `Season` round-robins `SS26 → FW26 → Resort26` by row index.
- Row 11's status changed from **Negotiation** to **PendingApproval** (the enum gained this value after the mockup review — the original 30-row sample predates it, so at least one row needs to exercise it).

| # | QuotationNo | QuotationDate | Buyer | Merchandiser | Unit | Value (USD) | Status | DaysOpen (source) | ConvertedDate | StyleNo | Season |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | QTN-2026-0001 | 2026-06-12 | H&M | Fatema Begum | Unit-1 | 185000 | Converted | 18 | 2026-06-30 | STY-4001 | SS26 |
| 2 | QTN-2026-0002 | 2026-06-15 | Zara | Jahid Hasan | Unit-2 | 92000 | Converted | 22 | 2026-07-07 | STY-4002 | FW26 |
| 3 | QTN-2026-0003 | 2026-06-18 | Primark | Mehedi Hasan | Unit-1 | 64000 | Rejected | 11 | — | STY-4003 | Resort26 |
| 4 | QTN-2026-0004 | 2026-06-20 | C&A | Sumaiya Akter | Unit-3 | 112000 | Converted | 15 | 2026-07-05 | STY-4004 | SS26 |
| 5 | QTN-2026-0005 | 2026-06-22 | Mango | Fatema Begum | Unit-1 | 78000 | Expired | 35 | — | STY-4005 | FW26 |
| 6 | QTN-2026-0006 | 2026-06-25 | H&M | Jahid Hasan | Unit-2 | 210000 | Converted | 9 | 2026-07-04 | STY-4006 | Resort26 |
| 7 | QTN-2026-0007 | 2026-06-28 | Next | Mehedi Hasan | Unit-1 | 45000 | Negotiation | 34 | — | STY-4007 | SS26 |
| 8 | QTN-2026-0008 | 2026-07-01 | Zara | Sumaiya Akter | Unit-2 | 156000 | Approved | 31 | — | STY-4008 | FW26 |
| 9 | QTN-2026-0009 | 2026-07-03 | Primark | Fatema Begum | Unit-1 | 89000 | Submitted | 29 | — | STY-4009 | Resort26 |
| 10 | QTN-2026-0010 | 2026-07-05 | C&A | Jahid Hasan | Unit-3 | 134000 | Converted | 12 | 2026-07-17 | STY-4010 | SS26 |
| 11 | QTN-2026-0011 | 2026-07-08 | H&M | Mehedi Hasan | Unit-1 | 198000 | PendingApproval | 24 | — | STY-4011 | FW26 |
| 12 | QTN-2026-0012 | 2026-07-10 | Mango | Sumaiya Akter | Unit-2 | 67000 | Draft | 22 | — | STY-4012 | Resort26 |
| 13 | QTN-2026-0013 | 2026-07-12 | Next | Fatema Begum | Unit-1 | 52000 | Converted | 8 | 2026-07-20 | STY-4013 | SS26 |
| 14 | QTN-2026-0014 | 2026-07-14 | Zara | Jahid Hasan | Unit-2 | 143000 | Approved | 18 | — | STY-4014 | FW26 |
| 15 | QTN-2026-0015 | 2026-07-16 | Primark | Mehedi Hasan | Unit-3 | 97000 | Submitted | 16 | — | STY-4015 | Resort26 |
| 16 | QTN-2026-0016 | 2026-07-18 | C&A | Sumaiya Akter | Unit-1 | 110000 | Negotiation | 14 | — | STY-4016 | SS26 |
| 17 | QTN-2026-0017 | 2026-07-20 | H&M | Fatema Begum | Unit-2 | 175000 | Converted | 7 | 2026-07-27 | STY-4017 | FW26 |
| 18 | QTN-2026-0018 | 2026-07-22 | Mango | Jahid Hasan | Unit-1 | 83000 | Rejected | 5 | — | STY-4018 | Resort26 |
| 19 | QTN-2026-0019 | 2026-07-24 | Next | Mehedi Hasan | Unit-3 | 61000 | Draft | 8 | — | STY-4019 | SS26 |
| 20 | QTN-2026-0020 | 2026-07-25 | Zara | Sumaiya Akter | Unit-2 | 129000 | Submitted | 7 | — | STY-4020 | FW26 |
| 21 | QTN-2026-0021 | 2026-07-26 | Primark | Fatema Begum | Unit-1 | 94000 | Negotiation | 6 | — | STY-4021 | Resort26 |
| 22 | QTN-2026-0022 | 2026-07-27 | C&A | Jahid Hasan | Unit-2 | 152000 | Approved | 5 | — | STY-4022 | SS26 |
| 23 | QTN-2026-0023 | 2026-07-28 | H&M | Mehedi Hasan | Unit-1 | 205000 | Submitted | 4 | — | STY-4023 | FW26 |
| 24 | QTN-2026-0024 | 2026-07-29 | Mango | Sumaiya Akter | Unit-3 | 72000 | Draft | 3 | — | STY-4024 | Resort26 |
| 25 | QTN-2026-0025 | 2026-07-30 | Next | Fatema Begum | Unit-1 | 48000 | Draft | 2 | — | STY-4025 | SS26 |
| 26 | QTN-2026-0026 | 2026-07-30 | Zara | Jahid Hasan | Unit-2 | 167000 | Submitted | 2 | — | STY-4026 | FW26 |
| 27 | QTN-2026-0027 | 2026-07-31 | Primark | Mehedi Hasan | Unit-1 | 88000 | Draft | 1 | — | STY-4027 | Resort26 |
| 28 | QTN-2026-0028 | 2026-07-31 | C&A | Sumaiya Akter | Unit-3 | 119000 | Draft | 1 | — | STY-4028 | SS26 |
| 29 | QTN-2026-0029 | 2026-08-01 | H&M | Fatema Begum | Unit-2 | 192000 | Draft | 0 | — | STY-4029 | FW26 |
| 30 | QTN-2026-0030 | 2026-08-01 | Mango | Jahid Hasan | Unit-1 | 76000 | Draft | 0 | — | STY-4030 | Resort26 |

`LostReason` for row 3 (Rejected): `"Price not competitive"`. Row 5 (Expired): `"Buyer did not respond before validity expired"`. Row 18 (Rejected): `"Buyer selected another vendor"`.

Each row's `BuyerId`/`MerchandiserId`/`UnitId` resolve via the name lookups above; `CreatedBy` = the seed system user GUID; `CurrencyCode` = `"USD"`.

---

## Quotation Items (generated, not hand-enumerated)

Every quotation gets exactly 2 line items, generated deterministically from `Value` so `SUM(Amount) == Quotation.Value` exactly (`Discount` is 0 for all seed rows, so `Value` already equals the subtotal):

```csharp
var qtyTotal = Math.Round(value / 6m / 100m) * 100m;      // ~$6/pc baseline, rounded to a round lot
var qty1 = Math.Round(qtyTotal * 0.6m / 100m) * 100m;
var amount1 = qty1 * 6.00m;
var amount2 = value - amount1;                             // exact remainder — no rounding drift
var qty2 = Math.Max(10m, Math.Round(amount2 / 6m / 10m) * 10m);
var unitPrice2 = Math.Round(amount2 / qty2, 2);
```

`ItemDescription` is picked from a per-`UnitType` pair (`Knit`: "Men's T-Shirt" / "Men's Polo Shirt"; `Woven`: "Men's Shirt" / "Men's Pant"; `Sweater`: "Men's Sweater" / "Men's Cardigan"). `StyleNo` per item = `{Quotation.StyleNo}-01` / `-02`.

---

## Quotation Status History (generated, not hand-enumerated)

Backs the Quotation Details Timeline widget. Forward progression is `Draft → Submitted → Negotiation → PendingApproval → Approved → Converted`; `Rejected`/`Expired` branch off after `Negotiation` (the source data doesn't record which stage a rejection happened at, so `Negotiation` is used as the last active stage before all seeded rejections/expiries):

```csharp
var forwardOrder = new[] { Draft, Submitted, Negotiation, PendingApproval, Approved, Converted };

var stages = (status is Rejected or Expired)
    ? new[] { Draft, Submitted, Negotiation, status }
    : forwardOrder[..(Array.IndexOf(forwardOrder, status) + 1)];

var endDate = status == Converted ? convertedDate!.Value : quotationDate.AddDays(daysOpenSource);
var startDate = quotationDate;

for (var i = 0; i < stages.Length; i++)
{
    var date = stages.Length == 1
        ? startDate
        : startDate.AddDays((endDate.DayNumber - startDate.DayNumber) * i / (stages.Length - 1));
    // insert QuotationStatusHistory { Status = stages[i], StatusDate = date }
}
```

---

## Seeding Mechanism

**Not** EF Core `HasData` in migrations — 30+ rows with computed dates don't belong baked into migration history, and `HasData` diffs awkwardly on every schema change. Instead:

- A `DatabaseSeeder` class in `Infrastructure/Persistence/EfCore/Seed/`, reading the table above from an embedded JSON resource (`seed-quotations.json`) — only the base columns (`QuotationNo`, `QuotationDate`, buyer/merchandiser/unit names, `Value`, `Status`, `DaysOpen`, `ConvertedDate`, `LostReason`, `StyleNo`, `Season`) live in the JSON; `Incoterm`/`PaymentTerm`/`Discount`/`ValidUntil`/items/status-history are all generated in code per the rules above.
- Executed once at startup, **guarded to Development only**:
  ```csharp
  if (app.Environment.IsDevelopment())
      await app.Services.GetRequiredService<DatabaseSeeder>().SeedAsync();
  ```
- Idempotent — upsert by natural key (`QuotationNo`, `BuyerName`, `UnitName`), so re-running on an already-seeded DB is a no-op rather than a duplicate insert. Items/status-history are only generated the first time a given `Quotation` is inserted — they aren't re-diffed on subsequent runs.
- After seeding, manually trigger `REFRESH MATERIALIZED VIEW bi.mv_sales_quotation_summary` once (or wait for the next `pg_cron` cycle) so the dashboards have data to show immediately in dev.
