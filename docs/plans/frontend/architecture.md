# SalesDeliveryBI — Frontend Architecture (Angular)

**Scope:** 3 dashboards — Quotation Pipeline, Conversion & Win/Loss, Aging. Read-only, filter-driven (unit, date range).

---

## Stack

- Angular 22 (latest) — standalone components + signals (no NgModule sprawl for new features). Verify AG Grid Angular wrapper + PrimeNG peer-dep compatibility with v22 before scaffolding; pin to the highest version both support if either lags.
- AG Grid Community + PrimeNG (grids from AG Grid, charts/cards/inputs from PrimeNG)
- No NgRx — dashboards are read-only, filter-driven views; a lightweight per-feature signal-based store is enough. Reassess only if cross-dashboard shared state grows complex.

---

## Folder Structure

```
src/app/
├── core/
│   ├── auth/              JWT interceptor, auth guard, current-user service
│   ├── http/              base API client, error interceptor (maps 403/404/500 → toast)
│   └── models/            shared DTOs mirrored from backend (QuotationPipelineDto, etc.)
│
├── shared/
│   ├── components/
│   │   ├── kpi-card/           reusable KPI card (label, value, trend)
│   │   ├── data-as-of/         "Data as of {lastRefresh}" badge — used on every dashboard
│   │   ├── unit-date-filter/   shared unit + date-range filter bar
│   │   └── status-badge/       color-coded status pill (Draft/Submitted/.../Converted)
│   └── pipes/                 currency-usd, days-open
│
├── features/
│   ├── quotation-pipeline/
│   │   ├── pipeline.page.ts        (standalone component, route target)
│   │   ├── pipeline.service.ts     (API calls + signal state)
│   │   └── pipeline-grid.component.ts   (AG Grid config for open quotations)
│   │
│   ├── quotation-conversion/
│   │   ├── conversion.page.ts
│   │   ├── conversion.service.ts
│   │   └── buyer-performance-grid.component.ts
│   │
│   └── quotation-aging/
│       ├── aging.page.ts
│       ├── aging.service.ts
│       └── aging-grid.component.ts
│
└── app.routes.ts
```

Each feature is self-contained: page + service + grid component. No feature imports another feature directly — shared pieces only come from `shared/`.

---

## Responsive Rules (non-negotiable, per repo `CLAUDE.md`)

- **KPI card row**: flex-wrap grid, never fixed-width; cards stack vertically below ~640px.
- **AG Grid columns**: each column declares a `minWidth`; low-priority columns (e.g. `createdBy`, `season`) are hidden below tablet width via a responsive column-state preset, not shrunk illegibly.
- **Filter bar**: unit/date pickers collapse into a single row on desktop, stack on mobile; never causes horizontal scroll on the page — only the grid itself scrolls horizontally, in its own container.
- **Charts** (conversion trend, win/loss bar): responsive container queries, not fixed pixel widths.
- Test every dashboard at mobile (375px), tablet (768px), and desktop (1280px) before marking a feature done.

---

## API Integration Pattern

- One Angular service per feature (`pipeline.service.ts`), calling the matching backend endpoint (`GET /api/sales/quotations/pipeline`) with unit/date query params.
- Every API response includes `lastRefresh` — service exposes it as a signal, `data-as-of` component reads it. Never hardcode or compute a client-side "last updated" time.
- JWT attached via a core HTTP interceptor; a 403 response is handled globally (redirect to "not authorized" state), not per-feature.
- Loading state: explicit skeleton/spinner while fetching — no blank screen, no flash of empty grid.

---

## Component Reuse Targets

| Component | Used by |
|---|---|
| `kpi-card` | All 3 dashboards |
| `data-as-of` | All 3 dashboards |
| `unit-date-filter` | Pipeline, Conversion (Aging uses unit-only, no date range) |
| `status-badge` | Pipeline grid, Aging grid |
