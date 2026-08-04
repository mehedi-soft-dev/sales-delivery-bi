import { DestroyRef, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, of, switchMap } from 'rxjs';
import type { PagedResult } from '../../core/models/dashboard.models';

export interface PagedQuerySignal<TFilter, TRow> {
  readonly page: Signal<PagedResult<TRow> | null>;
  readonly loading: Signal<boolean>;
  readonly error: Signal<unknown>;
  load(filter: TFilter): void;
}

/**
 * Same cancel-in-flight-request shape as `createQuerySignal` (shared/data/query-signal.ts), for endpoints
 * that return a plain `PagedResult<T>` with no `DashboardResponse` envelope/`lastRefresh` — the Admin
 * views (Application/Services/AdminAppService.cs) are uncached live OLTP reads, not BI dashboards.
 */
export function createPagedQuerySignal<TFilter, TRow>(
  fetch: (filter: TFilter) => Observable<PagedResult<TRow>>,
): PagedQuerySignal<TFilter, TRow> {
  const destroyRef = inject(DestroyRef);

  const page = signal<PagedResult<TRow> | null>(null);
  const loading = signal(false);
  const error = signal<unknown>(null);

  const filterRequests = new Subject<TFilter>();

  filterRequests
    .pipe(
      switchMap((filter) => {
        loading.set(true);
        error.set(null);
        return fetch(filter).pipe(
          catchError((err: unknown) => {
            error.set(err);
            loading.set(false);
            return of(null);
          }),
        );
      }),
      takeUntilDestroyed(destroyRef),
    )
    .subscribe((result) => {
      loading.set(false);
      if (result) {
        page.set(result);
      }
    });

  return {
    page: page.asReadonly(),
    loading: loading.asReadonly(),
    error: error.asReadonly(),
    load: (filter: TFilter) => filterRequests.next(filter),
  };
}
