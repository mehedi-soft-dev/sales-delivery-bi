import { DestroyRef, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, of, switchMap } from 'rxjs';
import type { DashboardResponse } from '../../core/models/dashboard.models';

export interface QuerySignal<TFilter, TData> {
  readonly data: Signal<TData | null>;
  readonly lastRefresh: Signal<string | null>;
  readonly loading: Signal<boolean>;
  readonly error: Signal<unknown>;
  load(filter: TFilter): void;
}

/**
 * Must be called from an injection context (e.g. a feature service's field initializer) —
 * it calls `inject(DestroyRef)` internally to unsubscribe when that service is destroyed.
 * Each `load()` call cancels any still-in-flight previous request (switchMap), so a fast
 * filter change can never have an older response overwrite a newer one.
 */
export function createQuerySignal<TFilter, TData>(
  fetch: (filter: TFilter) => Observable<DashboardResponse<TData>>,
): QuerySignal<TFilter, TData> {
  const destroyRef = inject(DestroyRef);

  const data = signal<TData | null>(null);
  const lastRefresh = signal<string | null>(null);
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
    .subscribe((response) => {
      loading.set(false);
      if (response) {
        data.set(response.data);
        lastRefresh.set(response.lastRefresh);
      }
    });

  return {
    data: data.asReadonly(),
    lastRefresh: lastRefresh.asReadonly(),
    loading: loading.asReadonly(),
    error: error.asReadonly(),
    load: (filter: TFilter) => filterRequests.next(filter),
  };
}
