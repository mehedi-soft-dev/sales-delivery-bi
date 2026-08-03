# Sales & Delivery Module – BI Developer Guidelines

**Version:** 1.0  
**Date:** 01 August 2026  
**Focus:** Sale Quotations (primary) + Sales Order → Delivery pipeline  
**Parent Document:** ERP_BI_Platform_Developer_Guidelines.md  

---

## 1. Module Overview

The **Sales & Delivery** module covers the complete order-to-delivery cycle in the Garment/Textile ERP:

| Sub-Module              | Description                                      | BI Priority |
|-------------------------|--------------------------------------------------|-------------|
| Sale Quotations         | Customer quotation creation, revision, approval  | **Very High** |
| Sales Orders            | Confirmed orders from quotations                 | High        |
| Delivery / Challan      | Goods delivery against orders                    | High        |
| Sales Invoice           | Commercial invoicing after delivery              | Medium-High |
| Return / Credit Note    | Sales returns                                    | Medium      |

This guideline focuses heavily on **Sale Quotations** because it is the starting point of the revenue pipeline and has high management visibility.

---

## 2. Sale Quotations – Business Process

Typical flow:

```
Draft → Submitted → Under Negotiation → Approved → Converted to Sales Order
                 ↘ Rejected / Expired / Lost
```

**Key Business Questions for BI:**
- How many quotations are pending approval?
- What is the quotation-to-order conversion rate?
- Which buyers / merchandisers have the highest win rate?
- What is the value of open quotations (pipeline)?
- Aging of quotations (how long they stay open)?
- Win/Loss analysis by reason, buyer, season, unit?

---

## 3. Recommended Materialized Views

### 3.1 Primary MV – Quotation Summary

```sql
-- Suggested name
MV_SALES_QUOTATION_SUMMARY

Key Columns:
- QUOTATION_ID
- QUOTATION_NO
- QUOTATION_DATE
- BUYER_ID / BUYER_NAME
- MERCHANDISER_ID / MERCHANDISER_NAME
- UNIT_ID / UNIT_NAME
- STYLE_NO / SEASON
- CURRENCY_CODE
- QUOTATION_VALUE_USD
- STATUS                  -- Draft, Submitted, Negotiation, Approved, Rejected, Expired, Converted
- STATUS_DATE
- DAYS_IN_STATUS
- DAYS_OPEN               -- from quotation date to today or conversion date
- CONVERTED_TO_SO_NO
- CONVERTED_DATE
- CONVERSION_DAYS
- LOST_REASON
- CREATED_BY
- LAST_REFRESH_DATE
```

### 3.2 Supporting MVs (optional)

- `MV_QUOTATION_PIPELINE_DAILY` → daily snapshot of open pipeline value by status
- `MV_QUOTATION_CONVERSION_RATE` → monthly conversion % by buyer / merchandiser / unit

---

## 4. BI Dashboards for Sale Quotations

### 4.1 Quotation Pipeline Dashboard (Operational)

**Purpose:** Daily working dashboard for Commercial / Merchandising team.

**Visual Mockup Description:**

```
+------------------------------------------------------------------+
|  Sale Quotation Pipeline                    [Unit] [Buyer] [Date] |
+------------------------------------------------------------------+
|  KPI Cards                                                        |
|  [Open Quotations: 24]  [Pipeline Value: $1.85M]                  |
|  [Pending Approval: 7]  [Avg Days Open: 12]                       |
+------------------------------------------------------------------+
|  Status Process Bar                                               |
|  Draft (5) → Submitted (8) → Negotiation (6) → Approved (4) → Converted (3) |
+------------------------------------------------------------------+
|  AG Grid – Open Quotations                                        |
|  Quotation # | Buyer | Merchandiser | Value | Status | Days Open  |
|  QTN-2026-0007 | Next | Mehedi | $45,000 | Negotiation | 34     |
|  QTN-2026-0009 | Primark | Fatema | $89,000 | Submitted | 29    |
|  ...                                                              |
+------------------------------------------------------------------+
```

**Key Features:**
- Color-coded status badges
- Clickable status bar segments (filter grid)
- Excel export from AG Grid
- “Data as of” timestamp

---

### 4.2 Quotation Conversion & Win/Loss Dashboard (Management)

**Purpose:** Performance analysis for managers.

**Visual Mockup Description:**

```
+------------------------------------------------------------------+
|  Quotation Conversion & Win/Loss Analysis                         |
+------------------------------------------------------------------+
|  KPI Cards                                                        |
|  [Conversion Rate: 68%]  [Won Value: $1.2M]                       |
|  [Lost Value: $0.45M]    [Avg Conversion Days: 14]                |
+------------------------------------------------------------------+
|  Left Chart: Monthly Conversion Rate Trend (Line)                 |
|  Right Chart: Win vs Lost Value by Month (Bar)                    |
+------------------------------------------------------------------+
|  AG Grid – Buyer Performance                                      |
|  Buyer | Quotations | Won | Lost | Conversion % | Value           |
+------------------------------------------------------------------+
```

**Key Features:**
- Trend comparison (current vs previous period)
- Drill-down from chart to buyer details
- Win/Loss reason analysis (optional secondary view)

---

### 4.3 Quotation Aging Dashboard

**Purpose:** Identify risk of delayed quotations.

**Visual Mockup Description:**

```
+------------------------------------------------------------------+
|  Sale Quotation Aging Analysis                                    |
+------------------------------------------------------------------+
|  KPI Cards: Total Open Value | High Risk Aged Value (>30 days)    |
+------------------------------------------------------------------+
|  Aging Buckets (Stacked / Grouped Bar)                            |
|  0-7 days | 8-15 days | 16-30 days | 31-60 days | 60+ days        |
+------------------------------------------------------------------+
|  AG Grid – Aged Quotations (sorted by Days Open DESC)             |
|  Quotation # | Buyer | Value | Days Open | Status | Risk Level    |
|  (Rows > 30 days highlighted in amber/red)                        |
+------------------------------------------------------------------+
```

**Key Features:**
- Conditional formatting on aging
- Quick filter for “High Risk only”
- Alert count for quotations > 30 days

---

## 5. Role-Based Access (Sales & Delivery)

| Role                  | Quotation Access                          | Notes |
|-----------------------|-------------------------------------------|-------|
| SuperAdmin            | Full                                      | All units |
| GeneralManager        | Full view + conversion analysis           | All units |
| CommercialManager     | Full (create/edit + all reports)          | Assigned units |
| CommercialOfficer     | Create/edit own + view team pipeline      | Own + team |
| Merchandiser          | Own quotations + limited pipeline         | Own only |
| FinanceManager        | View only (value & conversion)            | All units |
| Viewer                | Read-only summary                         | Limited |

**Row-level rule:** User can only see quotations of units assigned in `USER_UNITS`.

---

## 6. Redis Caching Guidelines (Sales Specific)

| Dashboard / Data              | TTL          | Key Example |
|-------------------------------|--------------|-------------|
| Quotation Pipeline Summary    | 3–5 min      | `bi:sales:quotation:pipeline:unit:1:2026-08-01` |
| Conversion Rate (Monthly)     | 10–15 min    | `bi:sales:quotation:conversion:unit:all:2026-07` |
| Aging Analysis                | 5 min        | `bi:sales:quotation:aging:unit:2` |
| Open Quotation List (Grid)    | 2–3 min      | Include filter hash in key |

---

## 7. Sample Data – Sale Quotations (30 records)

| QUOTATION_ID | QUOTATION_NO   | QUOTATION_DATE | BUYER_NAME          | MERCHANDISER     | UNIT_NAME     | VALUE_USD  | STATUS       | DAYS_OPEN | CONVERTED |
|--------------|----------------|----------------|---------------------|------------------|---------------|------------|--------------|-----------|-----------|
| 1001         | QTN-2026-0001  | 2026-06-12     | H&M                 | Fatema Begum     | Unit-1 (Knit) | 185000     | Converted    | 18        | Y         |
| 1002         | QTN-2026-0002  | 2026-06-15     | Zara                | Jahid Hasan      | Unit-2 (Woven)| 92000      | Converted    | 22        | Y         |
| 1003         | QTN-2026-0003  | 2026-06-18     | Primark             | Mehedi Hasan     | Unit-1 (Knit) | 64000      | Rejected     | 11        | N         |
| 1004         | QTN-2026-0004  | 2026-06-20     | C&A                 | Sumaiya Akter    | Unit-3 (Sweater)| 112000   | Converted    | 15        | Y         |
| 1005         | QTN-2026-0005  | 2026-06-22     | Mango               | Fatema Begum     | Unit-1 (Knit) | 78000      | Expired      | 35        | N         |
| 1006         | QTN-2026-0006  | 2026-06-25     | H&M                 | Jahid Hasan      | Unit-2 (Woven)| 210000     | Converted    | 9         | Y         |
| 1007         | QTN-2026-0007  | 2026-06-28     | Next                | Mehedi Hasan     | Unit-1 (Knit) | 45000      | Negotiation  | 34        | N         |
| 1008         | QTN-2026-0008  | 2026-07-01     | Zara                | Sumaiya Akter    | Unit-2 (Woven)| 156000     | Approved     | 31        | N         |
| 1009         | QTN-2026-0009  | 2026-07-03     | Primark             | Fatema Begum     | Unit-1 (Knit) | 89000      | Submitted    | 29        | N         |
| 1010         | QTN-2026-0010  | 2026-07-05     | C&A                 | Jahid Hasan      | Unit-3 (Sweater)| 134000   | Converted    | 12        | Y         |
| 1011         | QTN-2026-0011  | 2026-07-08     | H&M                 | Mehedi Hasan     | Unit-1 (Knit) | 198000     | Negotiation  | 24        | N         |
| 1012         | QTN-2026-0012  | 2026-07-10     | Mango               | Sumaiya Akter    | Unit-2 (Woven)| 67000      | Draft        | 22        | N         |
| 1013         | QTN-2026-0013  | 2026-07-12     | Next                | Fatema Begum     | Unit-1 (Knit) | 52000      | Converted    | 8         | Y         |
| 1014         | QTN-2026-0014  | 2026-07-14     | Zara                | Jahid Hasan      | Unit-2 (Woven)| 143000     | Approved     | 18        | N         |
| 1015         | QTN-2026-0015  | 2026-07-16     | Primark             | Mehedi Hasan     | Unit-3 (Sweater)| 97000    | Submitted    | 16        | N         |
| 1016         | QTN-2026-0016  | 2026-07-18     | C&A                 | Sumaiya Akter    | Unit-1 (Knit) | 110000     | Negotiation  | 14        | N         |
| 1017         | QTN-2026-0017  | 2026-07-20     | H&M                 | Fatema Begum     | Unit-2 (Woven)| 175000     | Converted    | 7         | Y         |
| 1018         | QTN-2026-0018  | 2026-07-22     | Mango               | Jahid Hasan      | Unit-1 (Knit) | 83000      | Rejected     | 5         | N         |
| 1019         | QTN-2026-0019  | 2026-07-24     | Next                | Mehedi Hasan     | Unit-3 (Sweater)| 61000    | Draft        | 8         | N         |
| 1020         | QTN-2026-0020  | 2026-07-25     | Zara                | Sumaiya Akter    | Unit-2 (Woven)| 129000     | Submitted    | 7         | N         |
| 1021         | QTN-2026-0021  | 2026-07-26     | Primark             | Fatema Begum     | Unit-1 (Knit) | 94000      | Negotiation  | 6         | N         |
| 1022         | QTN-2026-0022  | 2026-07-27     | C&A                 | Jahid Hasan      | Unit-2 (Woven)| 152000     | Approved     | 5         | N         |
| 1023         | QTN-2026-0023  | 2026-07-28     | H&M                 | Mehedi Hasan     | Unit-1 (Knit) | 205000     | Submitted    | 4         | N         |
| 1024         | QTN-2026-0024  | 2026-07-29     | Mango               | Sumaiya Akter    | Unit-3 (Sweater)| 72000    | Draft        | 3         | N         |
| 1025         | QTN-2026-0025  | 2026-07-30     | Next                | Fatema Begum     | Unit-1 (Knit) | 48000      | Draft        | 2         | N         |
| 1026         | QTN-2026-0026  | 2026-07-30     | Zara                | Jahid Hasan      | Unit-2 (Woven)| 167000     | Submitted    | 2         | N         |
| 1027         | QTN-2026-0027  | 2026-07-31     | Primark             | Mehedi Hasan     | Unit-1 (Knit) | 88000      | Draft        | 1         | N         |
| 1028         | QTN-2026-0028  | 2026-07-31     | C&A                 | Sumaiya Akter    | Unit-3 (Sweater)| 119000   | Draft        | 1         | N         |
| 1029         | QTN-2026-0029  | 2026-08-01     | H&M                 | Fatema Begum     | Unit-2 (Woven)| 192000     | Draft        | 0         | N         |
| 1030         | QTN-2026-0030  | 2026-08-01     | Mango               | Jahid Hasan      | Unit-1 (Knit) | 76000      | Draft        | 0         | N         |

---

## 8. Suggested API Endpoints (ASP.NET Core)

```
GET /api/sales/quotations/pipeline
GET /api/sales/quotations/conversion
GET /api/sales/quotations/aging
GET /api/sales/quotations/{id}
GET /api/sales/quotations/summary          // for Executive dashboard KPI
```

All endpoints must:
- Accept Unit + Date filters
- Apply role-based + unit-based security
- Support Redis caching
- Return `lastRefresh` timestamp

---

## 9. Integration with Executive Dashboard

From the Sale Quotations module, feed these KPIs into the main **Executive Overview**:

- Open Pipeline Value (USD)
- Quotation Conversion Rate (MTD)
- High-value quotations pending > 15 days (alert)

---

## 10. Development Checklist

- [ ] Create `MV_SALES_QUOTATION_SUMMARY`
- [ ] Build Quotation Pipeline dashboard (Angular + AG Grid)
- [ ] Implement conversion & aging reports
- [ ] Apply role + unit security
- [ ] Add Redis caching with correct keys
- [ ] Show “Data as of” on every screen
- [ ] Link from Executive Overview KPI cards

---

**End of Sales & Delivery (Sale Quotations) Guidelines**
