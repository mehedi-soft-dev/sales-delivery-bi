# SalesDeliveryBI — Frontend Implementation Checklist

Phased build order, mirrors `docs/plans/backend/checklist.md`. Each phase should be functionally complete and buildable before moving to the next — don't skip ahead with stubs. Follows `docs/plans/frontend/architecture.md` exactly; flag any deviation before implementing.

---

## Phase 0 — Project Setup

- [x] `ng new` Angular 22 app in `src/frontend` — standalone components, signals, no NgModule scaffolding (Angular CLI 22.1.2; required bumping local Node.js from v24.14.1 → v24.18.1 LTS via winget, confirmed with the user first since it's a system-wide install)
- [x] Add AG Grid Community, PrimeNG, `ng-apexcharts` — verify peer-dep compatibility with Angular 22 first; pin to the highest version all three support if one lags (`ag-grid-angular`/`ag-grid-community` 36.0.2, `primeng` 22.0.0 + `@angular/cdk` 22.1.0, `ng-apexcharts` 2.4.0 + `apexcharts` pinned to 5.16.0 — latest `apexcharts` is 6.x but `ng-apexcharts`'s peer range is `^5.10.3`, so 6.x was deliberately avoided)
- [x] Add PrimeNG theme + icon set; confirm it doesn't fight AG Grid's theme — `providePrimeNG({ theme: { preset: Aura } })` wired in `app.config.ts` via `@primeuix/themes`; icons come from `@primeicons/angular` (component-based, bundled as a `primeng` dependency, no separate CSS import needed); also added `@angular/animations` + `provideAnimationsAsync()` — PrimeNG overlay components still need it even though the classic Animations API is deprecated in v22. AG Grid 36 uses the JS Theming API (no global CSS), so the actual grid theme (e.g. `themeQuartz` tuned to match Aura) is picked when the first grid component is built in Phase 3/4, not here.
- [x] `.editorconfig` / ESLint / Prettier config — `.editorconfig`/`.prettierrc` scaffolded by `ng new`; added `@angular-eslint/schematics` via `ng add` for `eslint.config.js` + the `lint` architect target
- [x] `environment.ts` / `environment.development.ts` — generated via `ng generate environments`; `apiBaseUrl` set to `/api` (prod, same-origin/reverse-proxy) and `https://localhost:7041/api` (dev, matches the backend's `launchSettings.json` HTTPS profile)
- [x] Create folder structure per `architecture.md` — `features/quotation-pipeline`, `features/quotation-conversion`, `features/quotation-aging` created now (each holding a minimal real page component, needed so the router has something to lazy-load); `core/`'s and `shared/`'s deeper subfolders (`auth`, `http`, `models`, `components`, `charts`, `pipes`, `data`) are intentionally **not** pre-created empty — per `CLAUDE.md`'s "no placeholders" rule they're created with real content in Phase 2/3 instead. Added `core/not-found/not-found.page.ts` (not in `architecture.md`, needed for the wildcard route below).
- [x] `app.routes.ts` — 3 lazy routes (`loadComponent`) for pipeline/conversion/aging, default redirect `''` → `pipeline`, wildcard `**` → the not-found page
- [x] Confirm `ng build` and `ng serve` both run clean on the empty scaffold before adding features — `ng lint` clean, `ng build --configuration production` produces one small initial chunk + 4 separate lazy chunks (confirms per-route code-splitting from day one), `ng test` passes, and all 4 routes (`/pipeline`, `/conversion`, `/aging`, unknown → not-found) verified live via `ng serve`. Also stripped the default Angular marketing template from `app.html`/`app.ts`/`app.spec.ts` down to a bare `<router-outlet />` shell — the real layout lands in Phase 1.

## Phase 1 — App Shell / Layout (professional, responsive)

- [x] `layout/shell.ts` wrapping every dashboard route (nested under a parent route with `component: Shell` + children, not `app.ts` itself) — CSS Grid: topbar + sidebar + content + footer, not ad-hoc per-page layout. `app.html`/`app.ts` stripped down to a bare `<router-outlet />` — the default Angular marketing template is fully gone.
- [x] **Topbar** (`layout/topbar`): brand (left) + hamburger (mobile only), unit indicator + user avatar/menu (right); sticky (`position: sticky; top: 0`), fixed `--topbar-height: 64px`, no layout shift. `unitLabel` defaults to `'All Units'` (the real semantic default for a null/unset `unitId` per the API contract, not a fake placeholder) and `userDisplayName` defaults to `null` (renders a generic user icon) — both are component inputs Phase 2's `core/auth` will bind to real values; Topbar itself is fully functional today in the not-yet-authenticated state.
- [x] **Sidebar** (`layout/sidebar`): nav links to Pipeline/Conversion/Aging with icons (`@primeicons/angular`) + `routerLinkActive` highlighting; a single `<ng-template #navList>` is reused via `NgTemplateOutlet` in both the static aside (tablet rail/desktop full) and the mobile drawer, so the nav markup isn't duplicated. Verified: hidden `<768px` (drawer only), 64px icon-only rail at 768–1023px, 240px full-label at 1024px+, no horizontal scroll at any width.
- [x] **Content area**: `<main class="shell__content">` + `<router-outlet />`; consistent `max-width: 1440px` + padding across all 3 dashboard routes (page-title/breadcrumb region deferred to each dashboard page in Phase 4-6, since there's no real page title content yet).
- [x] **Footer** (`layout/footer`): app name + version sourced from `environment.version` (not a hardcoded string) — centered, stacks fine on mobile, never overlaps content (`grid-area: footer`, own row).
- [x] Use PrimeNG layout primitives (`Toolbar`, `Drawer`) for topbar/sidebar — **deviation**: `Menu` (popup) is NOT used for the user-menu dropdown. PrimeNG 22.0.0's `Menu` has a confirmed upstream bug (`parentId_r3 is not defined`, thrown from its own compiled `@for` track function inside the recursive-items template — reproduced with a single flat `MenuItem`, unrelated to our usage). Replaced with a small hand-rolled dropdown (`signal` + `@HostListener('document:click'/'document:keydown.escape')` to close on outside-click/Escape) — revisit and switch back to `p-menu` once PrimeNG ships a patch past 22.0.0. The sidebar's nav list is also intentionally plain `<a routerLink>` elements rather than PrimeNG `Menu`'s `MenuItem[]` model — simpler and gives correct `routerLinkActive` highlighting directly, with no generic-model indirection for 3 static links.
- [x] Global loading indicator — `core/http/loading-indicator.ts` (`LoadingIndicatorService` + `loadingIndicatorInterceptor`, a pending-request counter registered via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`); Shell renders a fixed top progress bar whenever `isLoading()` is true. Real today even though nothing calls the backend yet — Phase 2/4-6's HTTP calls will make it visible.
- [x] Global 403 (`core/not-authorized`, routed at `/403`, own layout — verified no topbar/sidebar render on it) and 404/not-found (`core/not-found`, wildcard route, own layout, link back to `/pipeline`) — both outside the `Shell` route so they never inherit dashboard chrome.
- [x] Verified the shell at mobile (375px), tablet (768px), desktop (1280px) via live `ng serve` + browser DOM/computed-style checks: sidebar rail/drawer/full-width switch correctly, hamburger only shows <768px, user-menu dropdown opens/closes (click + outside-click + Escape), active-link highlighting, zero horizontal scroll at any breakpoint, zero console errors. Raised the initial-bundle warning budget in `angular.json` (500kB → 700kB) to reflect the real cost of Toolbar/Drawer/Avatar/animations — actual initial chunk is 572kB, still well under the 1MB hard error.

## Phase 2 — Core Services

- [x] `core/auth` — `jwt.ts` (base64url decode + `exp` check, no signature verification client-side — that's the backend's job), `current-user.service.ts` (`CurrentUserService`: `sub`/`permissions`/`userUnits`/`isAuthenticated` signals read from `localStorage['sdbi_auth_token']`, `setToken`/`clearToken`/`hasPermission`), `auth.guard.ts` (`CanActivateFn`, applied via `canActivate: [authGuard]` on the `Shell` route in `app.routes.ts`). **Known limitation, called out explicitly**: the Identity service that issues these tokens doesn't exist yet (external dependency, same open item as the backend checklist's Phase 7) and login is out of scope for this repo per `CLAUDE.md` §8 — so today the guard redirects an unauthenticated visitor to `/403` (there's no distinct "please log in" page to send them to instead). Revisit once the Identity service defines its actual login/redirect flow. `Topbar`'s `unitLabel`/`userDisplayName` inputs are still unbound (Shell doesn't pass them yet) — there's no unit-name lookup (Phase 3) or a `name` claim in the JWT contract to bind to.
- [x] `core/http` — `auth.interceptor.ts` (attaches `Authorization: Bearer` only to requests whose URL starts with `environment.apiBaseUrl`), `error.interceptor.ts` (403 → `router.navigateByUrl('/403')`, never a toast; 404/500 → `MessageService.add(...)`, never a redirect). Registered in `app.config.ts` as `withInterceptors([loadingIndicatorInterceptor, errorInterceptor, authInterceptor])` — `errorInterceptor` first (outermost) so its `catchError` sees failures from the whole chain; `MessageService` provided at the root (`app.config.ts`) so the interceptor and the single `<p-toast>` in `app.html` share the same instance. `Toast`'s `@for` loop tracks by `msg` directly (no nested-context variable), unlike the broken `Menu` from Phase 1 — verified working live.
- [x] `core/models` — genuinely **generated**, not hand-mirrored: started the backend (`dotnet run`, against the already-running dev Postgres/Redis containers), fetched the live `/openapi/v1.json`, ran `openapi-typescript` against it → `api-schema.d.ts` (git-ignored from lint via `eslint.config.js`, regenerate with `npm run generate:api-types`). `dashboard.models.ts` re-exports typed aliases (`QuotationPipelineDto`, `ConversionDto`, `AgingDto`, etc.) plus the shared `DashboardResponse<T>` envelope — zero hand-typed fields, so no drift risk. This caught a real detail `api-contract.md` doesn't mention: numeric fields are typed `number | string` (System.Text.Json's OpenAPI schema for C# `decimal`), which the hand-written contract doc glosses over as plain numbers.
- [x] `shared/data/query-signal.ts` — `createQuerySignal(fetchFn)` returns `{ data, lastRefresh, loading, error, load() }`; internally a `Subject` piped through `switchMap` (cancels a still-in-flight request when `load()` is called again before it resolves) + `takeUntilDestroyed(inject(DestroyRef))`. Must be called from an injection context (e.g. a feature service's field initializer) — documented in a JSDoc comment since it's a non-obvious constraint. Verified with a dedicated spec: an older in-flight response arriving after a newer `load()` call is confirmed discarded (`query-signal.spec.ts`).
- [x] Confirmed interceptor JWT attachment two ways: (1) `auth.interceptor.spec.ts` via `HttpTestingController` — header present for `environment.apiBaseUrl`-prefixed requests with a token, absent with no token, absent for a non-API request (`/assets/logo.png`) even with a token set; (2) live end-to-end — minted a real dev-signing-key JWT (matching `appsettings.Development.json`) and hit the running backend directly with `curl`: own-unit request → `200` with real KPI data, foreign-unit request → `403` Problem Details, no token → `401`. Also verified live in the browser: no token in `localStorage` → `/pipeline` redirects to `/403`; valid token set → `/pipeline` loads. `error.interceptor.spec.ts` covers the 403/404/500 branching. 17/17 tests pass, `ng lint` clean, `ng build --configuration production` clean (468kB initial, back under the 500kB budget after Phase 1's `p-menu` removal).

## Phase 3 — Shared Components

- [ ] `kpi-card` — label/value/trend, flex-wrap row, stacks on mobile
- [ ] `data-as-of` — reads `lastRefresh` signal from the feature service, never hardcoded/computed client-side
- [ ] `unit-date-filter` — unit dropdown populated only from the caller's `user_units` claim; date range with debounced change emit; collapses to stacked layout on mobile
- [ ] `status-badge` — color-coded pill (Draft/Submitted/…/Converted) with a non-color indicator (icon/label) alongside color, for accessibility
- [ ] `apex-chart-theme.ts` — shared palette/fonts/responsive breakpoints, imported by every chart component
- [ ] `currency-usd`, `days-open` pipes
- [ ] Skeleton/spinner components for loading state — used by all 3 dashboards, no per-feature reimplementation

## Phase 4 — Feature: Quotation Pipeline

- [ ] `pipeline.service.ts` on `query-signal.ts`, calling `GET /api/sales/quotations/pipeline`
- [ ] `pipeline-grid.component.ts` — AG Grid, responsive column visibility (low-priority columns hidden below tablet, never shrunk illegibly)
- [ ] `pipeline.page.ts` — KPI cards + filter bar + grid, using the Phase 1 content-area layout
- [ ] Loading skeleton, empty-state (zero rows after filtering), and error state all handled explicitly — no blank screen

## Phase 5 — Feature: Conversion & Win/Loss

- [ ] `conversion.service.ts` on `query-signal.ts`, calling the conversion endpoint
- [ ] `conversion-trend-chart.component.ts` (ApexCharts line) + `win-loss-chart.component.ts` (ApexCharts bar), both on `apex-chart-theme.ts`
- [ ] `buyer-performance-grid.component.ts` — AG Grid with responsive columns
- [ ] `conversion.page.ts` — KPI cards + filter bar + charts + grid
- [ ] Loading/empty/error states handled explicitly

## Phase 6 — Feature: Aging

- [ ] `aging.service.ts` on `query-signal.ts`, calling the aging endpoint (unit-only filter, no date range per `architecture.md`)
- [ ] `aging-bucket-chart.component.ts` (ApexCharts bar — 0-7/8-15/16-30/31-60/60+)
- [ ] `aging-grid.component.ts` — AG Grid with `status-badge`, responsive columns
- [ ] `aging.page.ts` — assembled with unit-only filter bar
- [ ] Loading/empty/error states handled explicitly

## Phase 7 — Cross-Cutting Verification

- [ ] All 3 dashboards re-tested at mobile/tablet/desktop with real data (not just the empty shell from Phase 1)
- [ ] Rapid filter changes on each dashboard confirmed to not race (switch unit/date quickly, verify final displayed data matches the final filter, not an earlier one)
- [ ] 403 end-to-end: request a unit outside the seeded test user's `user_units` claim, confirm the not-authorized state renders (not a toast, not a blank grid)
- [ ] `data-as-of` value confirmed to match the backend response's `lastRefresh` exactly on all 3 dashboards, including after a cache warm-up refresh
- [ ] Lazy-loaded route bundles confirmed via build output (each feature's AG Grid/ApexCharts weight isolated to its own chunk)
- [ ] Accessibility pass: color-blind-safe status badges, keyboard navigation through sidebar/filter/grid, chart data also reachable without relying on hover-only tooltips

## Phase 8 — Testing

- [ ] Component tests: `data-as-of`, global 403 state, `unit-date-filter` (claim-scoped options, debounce)
- [ ] Service tests: each feature service's `query-signal.ts` flow, including cancel-on-new-filter behavior
- [ ] AG Grid responsive column-state presets and ApexCharts `responsive` breakpoint configs — one test per dashboard per breakpoint

## Phase 9 — Hardening / Polish

- [ ] Production build (`ng build --configuration production`) — confirm no console errors, bundle sizes reviewed
- [ ] Lighthouse pass on all 3 dashboards (performance + accessibility) at desktop and mobile
- [ ] Final visual QA against the Phase 1 shell — consistent spacing/typography/palette across all 3 dashboards, no per-page style drift
