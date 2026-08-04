# SalesDeliveryBI — Frontend Architecture (Angular)

**Scope:** 3 dashboards — Quotation Pipeline, Conversion & Win/Loss, Aging. Read-only, filter-driven (unit, date range).

---

## Stack

- Angular 21 — standalone components + signals (no NgModule sprawl for new features). Verify AG Grid Angular wrapper + PrimeNG + `ng-apexcharts` peer-dep compatibility with v21 before scaffolding; pin to the highest version all three support if one lags. **Not Angular 22**: PrimeNG 22.x requires a paid license key with no pre-license version supporting Angular 22 — see `CLAUDE.md` §4 and `checklist.md` Phase 0.
- AG Grid Community (grids) + PrimeNG (cards/inputs) + **ApexCharts** via `ng-apexcharts` (all charts — conversion trend line, win/loss bar, aging buckets bar)
- No NgRx — dashboards are read-only, filter-driven views; a lightweight per-feature signal-based store is enough. Reassess only if cross-dashboard shared state grows complex.

**Priority: access and caching over polish.** The two things this frontend must never get wrong are (1) a `403` from the API always renders as a distinct "not authorized" state, never a generic error or a silently empty dashboard, and (2) `lastRefresh`/`data-as-of` always reflects the real cached value from the response, never a client-computed or hardcoded timestamp. Everything else (chart choice, styling, layout polish) is secondary to these two.

---

## Folder Structure

```
src/app/
├── layout/
│   ├── shell.ts               root layout wrapping every dashboard route — topbar + sidebar + content + footer, sticky loading bar
│   ├── topbar/                brand, hamburger (mobile), unit indicator, user avatar/menu — PrimeNG Toolbar
│   ├── sidebar/                nav to the 3 dashboards, active-route highlighting; static aside (tablet/desktop) + PrimeNG Drawer (mobile overlay)
│   └── footer/                minimal app name/version
│
├── core/
│   ├── auth/              JWT interceptor, auth guard, current-user service, `user_units` claim reader
│   ├── http/              base API client, error interceptor (maps 404/500 → toast; 403 → not-authorized state, never a toast), loading-indicator interceptor
│   ├── models/            DTOs generated from `docs/plans/api-contract.md` (OpenAPI) — never hand-mirrored
│   ├── not-authorized/    full-page 403 state, own layout (no topbar/sidebar) — routed at `/403`
│   └── not-found/         full-page 404 state, own layout — wildcard route
│
├── shared/
│   ├── components/
│   │   ├── kpi-card/           reusable KPI card (label, value, trend)
│   │   ├── data-as-of/         "Data as of {lastRefresh}" badge — used on every dashboard
│   │   ├── unit-date-filter/   shared unit + date-range filter bar
│   │   └── status-badge/       color-coded status pill (Draft/Submitted/.../Converted)
│   ├── charts/
│   │   └── apex-chart-theme.ts   shared ApexCharts defaults (palette, responsive breakpoints, fonts) — every chart imports this, no per-chart ad-hoc styling
│   ├── pipes/                 currency-usd, days-open
│   └── data/
│       └── query-signal.ts    generic loading/error/data/lastRefresh signal helper — every feature service builds on this instead of re-declaring the same four signals
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
│   │   ├── buyer-performance-grid.component.ts
│   │   ├── conversion-trend-chart.component.ts   (ApexCharts line — monthly conversion rate)
│   │   └── win-loss-chart.component.ts           (ApexCharts bar — won vs lost value by month)
│   │
│   └── quotation-aging/
│       ├── aging.page.ts
│       ├── aging.service.ts
│       ├── aging-grid.component.ts
│       └── aging-bucket-chart.component.ts       (ApexCharts bar — 0-7/8-15/16-30/31-60/60+ buckets)
│
└── app.routes.ts
```

Each feature is self-contained: page + service + grid component. No feature imports another feature directly — shared pieces only come from `shared/`.

Each feature route in `app.routes.ts` uses `loadComponent` (lazy) — AG Grid and ApexCharts are heavy; no dashboard's JS/vendor bundle should load until its route is visited.

---

## Responsive Rules (non-negotiable, per repo `CLAUDE.md`)

- **KPI card row**: flex-wrap grid, never fixed-width; cards stack vertically below ~640px.
- **AG Grid columns**: each column declares a `minWidth`; low-priority columns (e.g. `createdBy`, `season`) are hidden below tablet width via a responsive column-state preset, not shrunk illegibly.
- **Filter bar**: unit/date pickers collapse into a single row on desktop, stack on mobile; never causes horizontal scroll on the page — only the grid itself scrolls horizontally, in its own container.
- **Charts** (ApexCharts — conversion trend, win/loss bar, aging buckets): use `chart.width: '100%'` / responsive container queries, not fixed pixel widths; verify each chart's own `responsive` breakpoint config against the mobile/tablet/desktop set below.
- Test every dashboard at mobile (375px), tablet (768px), and desktop (1280px) before marking a feature done.

---

## API Integration Pattern

- One Angular service per feature (`pipeline.service.ts`), calling the matching backend endpoint (`GET /api/sales/quotations/pipeline`) with unit/date query params, built on the shared `query-signal.ts` helper (loading/error/data/lastRefresh) instead of re-declaring those signals per feature.
- Every API response includes `lastRefresh` — service exposes it as a signal, `data-as-of` component reads it. Never hardcode or compute a client-side "last updated" time.
- JWT attached via a core HTTP interceptor; a 403 response is handled globally by the interceptor redirecting to a distinct "not authorized" state (never a toast, never folded into the generic error path) — this is the same rule for every feature, not re-implemented per dashboard.
- Filter changes (`unit-date-filter`) debounce date input and cancel any in-flight request for the previous filter value (`switchMap`-style) before firing the next one — prevents a stale response from a slow prior request overwriting the current filter's data.
- `unit-date-filter`'s unit dropdown is populated only from the caller's `user_units` JWT claim (via `core/auth`), never an open/unfiltered unit list — the picker should never offer a unit the user will just get a 403 for.
- Loading state: explicit skeleton/spinner while fetching — no blank screen, no flash of empty grid.

---

## Testing

- `data-as-of` and the global 403 "not authorized" state each get a component test — these are the two non-negotiable behaviors per repo `CLAUDE.md`, a regression here is a caching/access bug, not cosmetic.
- Each feature service (`pipeline.service.ts`, etc.) gets a unit test around the `query-signal.ts` loading/error/data flow, including the filter-change cancellation behavior.
- AG Grid column configs (responsive `minWidth`/hide presets) and chart `responsive` breakpoint configs get a test per dashboard at the mobile/tablet/desktop widths listed above.

## Component Reuse Targets

| Component | Used by |
|---|---|
| `kpi-card` | All 3 dashboards |
| `data-as-of` | All 3 dashboards |
| `unit-date-filter` | Pipeline, Conversion (Aging uses unit-only, no date range) |
| `status-badge` | Pipeline grid, Aging grid |
| `apex-chart-theme` | Conversion (trend + win/loss charts), Aging (bucket chart) |
