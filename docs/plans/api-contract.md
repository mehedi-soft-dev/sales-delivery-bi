# SalesDeliveryBI — API Contract

**Base path:** `/api/sales/quotations`
**Auth:** JWT bearer, required on every endpoint. Claims: `role`, `user_units` (array of unit ID **GUIDs**), `sub` (caller's user ID, GUID).

---

## Common Rules (every endpoint)

- All IDs (`unitId`, `quotationId`, `buyerId`, etc.) are **GUIDs**, serialized as lowercase string, e.g. `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` — no integer IDs anywhere in this API.
- Query params: `unitId` (guid, optional — omitted means "all units assigned to caller"), `fromDate`, `toDate` (ISO 8601 date, optional, endpoint-specific defaults apply).
- `unitId` is re-validated server-side against the caller's `user_units` claim — a value outside the caller's assignment returns `403`, not an empty result.
- Every successful response includes `lastRefresh` (ISO 8601 timestamp, from `bi.mv_refresh_log`) alongside the payload.
- Error format: [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807) — `{ type, title, status, detail, traceId }`.
- Cached via Redis; identical filter params within the TTL window return the same payload without hitting Postgres.

---

## 1. `GET /api/sales/quotations/pipeline`

KPI cards + open-quotations grid for the Pipeline dashboard.

**Query params:** `unitId?`

**Response 200:**
```json
{
  "data": {
    "kpis": {
      "openQuotationsCount": 24,
      "pipelineValueUsd": 1850000,
      "pendingApprovalCount": 7,
      "avgDaysOpen": 12
    },
    "statusFunnel": [
      { "status": "Draft", "count": 5 },
      { "status": "Submitted", "count": 8 },
      { "status": "Negotiation", "count": 6 },
      { "status": "PendingApproval", "count": 7 },
      { "status": "Approved", "count": 4 },
      { "status": "Converted", "count": 3 }
    ],
    "openQuotations": [
      {
        "quotationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "quotationNo": "QTN-2026-0007",
        "buyerName": "Next",
        "merchandiserName": "Mehedi Hasan",
        "valueUsd": 45000,
        "status": "Negotiation",
        "daysOpen": 34
      }
    ]
  },
  "lastRefresh": "2026-08-03T09:12:00Z"
}
```

---

## 2. `GET /api/sales/quotations/conversion`

KPI cards + trend + buyer performance grid for the Conversion & Win/Loss dashboard.

**Query params:** `unitId?`, `fromDate?`, `toDate?` (default: current month)

**Response 200:**
```json
{
  "data": {
    "kpis": {
      "conversionRatePct": 68,
      "wonValueUsd": 1200000,
      "lostValueUsd": 450000,
      "avgConversionDays": 14
    },
    "monthlyTrend": [
      { "month": "2026-06", "conversionRatePct": 62 },
      { "month": "2026-07", "conversionRatePct": 68 }
    ],
    "buyerPerformance": [
      {
        "buyerName": "H&M",
        "quotationsCount": 6,
        "wonCount": 4,
        "lostCount": 1,
        "conversionRatePct": 67,
        "valueUsd": 780000
      }
    ]
  },
  "lastRefresh": "2026-08-03T09:00:00Z"
}
```

---

## 3. `GET /api/sales/quotations/aging`

KPI cards + aging buckets + aged-quotations grid for the Aging dashboard.

**Query params:** `unitId?`

**Response 200:**
```json
{
  "data": {
    "kpis": {
      "totalOpenValueUsd": 1850000,
      "highRiskAgedValueUsd": 340000
    },
    "agingBuckets": [
      { "bucket": "0-7", "count": 10, "valueUsd": 620000 },
      { "bucket": "8-15", "count": 6, "valueUsd": 410000 },
      { "bucket": "16-30", "count": 5, "valueUsd": 480000 },
      { "bucket": "31-60", "count": 2, "valueUsd": 240000 },
      { "bucket": "60+", "count": 1, "valueUsd": 100000 }
    ],
    "agedQuotations": [
      {
        "quotationId": "9c4d2e11-8a2b-4f3e-9a1a-5b6c7d8e9f01",
        "quotationNo": "QTN-2026-0005",
        "buyerName": "Mango",
        "valueUsd": 78000,
        "daysOpen": 35,
        "status": "Expired",
        "riskLevel": "High"
      }
    ]
  },
  "lastRefresh": "2026-08-03T09:05:00Z"
}
```

---

## 4. `GET /api/sales/quotations/{id}`

Single quotation detail. Header fields come from `mv_sales_quotation_summary`; `items` and `statusHistory` are queried directly from `sales.QuotationItems`/`sales.QuotationStatusHistories` (Dapper), not from the MVs. `discountUsd`/`subtotalUsd` are FX-converted like the rest of the header; `items[].unitPrice`/`amount` are **not** — they're in `currencyCode`, matching the OLTP line-item rows (only the header total gets FX-converted today).

**Response 200:**
```json
{
  "data": {
    "quotationId": "9c4d2e11-8a2b-4f3e-9a1a-5b6c7d8e9f01",
    "quotationNo": "QTN-2026-0007",
    "quotationDate": "2026-06-28",
    "buyerName": "Next",
    "merchandiserName": "Mehedi Hasan",
    "unitName": "Unit-1 (Knit)",
    "styleNo": "STY-4021",
    "season": "SS26",
    "currencyCode": "USD",
    "quotationValueUsd": 45000,
    "incoterm": "FOB",
    "paymentTerm": "30 Days",
    "validUntil": "2026-07-27",
    "discountUsd": 5000,
    "subtotalUsd": 50000,
    "status": "Negotiation",
    "statusDate": "2026-07-15",
    "daysInStatus": 19,
    "daysOpen": 34,
    "convertedToSoNo": null,
    "convertedDate": null,
    "conversionDays": null,
    "lostReason": null,
    "createdBy": "7b2e1a4c-1234-4a5b-8c9d-0e1f2a3b4c5d",
    "items": [
      { "styleNo": "ST-1001", "itemDescription": "Men's Shirt", "qty": 5000, "unitPrice": 5.50, "amount": 27500 },
      { "styleNo": "ST-1002", "itemDescription": "Men's Pant", "qty": 3000, "unitPrice": 6.00, "amount": 18000 }
    ],
    "statusHistory": [
      { "status": "Draft", "statusDate": "2026-06-28" },
      { "status": "Submitted", "statusDate": "2026-06-29" },
      { "status": "Negotiation", "statusDate": "2026-07-02" }
    ]
  },
  "lastRefresh": "2026-08-03T09:12:00Z"
}
```

**Response 404:** quotation not found or not in caller's assigned units (returned as 404, not 403 — avoids confirming existence of a record outside the caller's access).

---

## 5. `GET /api/sales/quotations/summary`

Compact KPI set for the Executive Overview dashboard feed.

**Query params:** `unitId?`

**Response 200:**
```json
{
  "data": {
    "openPipelineValueUsd": 1850000,
    "conversionRateMtdPct": 68,
    "highValueAgedAlertCount": 3
  },
  "lastRefresh": "2026-08-03T09:00:00Z"
}
```

`highValueAgedAlertCount` = count of open quotations with `valueUsd` above a configurable threshold AND `daysOpen > 15`.
