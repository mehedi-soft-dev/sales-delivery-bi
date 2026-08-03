import { TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import type { DashboardResponse } from '../../core/models/dashboard.models';
import { createQuerySignal } from './query-signal';

describe('createQuerySignal', () => {
  it('cancels a still-in-flight request when a new filter is loaded (switchMap)', () => {
    const responses = new Map<string, Subject<DashboardResponse<string>>>();
    responses.set('a', new Subject());
    responses.set('b', new Subject());

    const query = TestBed.runInInjectionContext(() =>
      createQuerySignal<string, string>((filter) => responses.get(filter)!.asObservable()),
    );

    query.load('a');
    query.load('b');

    responses.get('a')!.next({ data: 'stale-a', lastRefresh: '2026-01-01T00:00:00Z' });
    expect(query.data()).toBeNull();

    responses.get('b')!.next({ data: 'fresh-b', lastRefresh: '2026-01-02T00:00:00Z' });
    expect(query.data()).toBe('fresh-b');
    expect(query.lastRefresh()).toBe('2026-01-02T00:00:00Z');
  });

  it('tracks loading state and clears it on success', () => {
    const response$ = new Subject<DashboardResponse<string>>();
    const query = TestBed.runInInjectionContext(() => createQuerySignal<void, string>(() => response$.asObservable()));

    expect(query.loading()).toBe(false);
    query.load(undefined);
    expect(query.loading()).toBe(true);

    response$.next({ data: 'ok', lastRefresh: '2026-01-01T00:00:00Z' });
    expect(query.loading()).toBe(false);
  });

  it('captures an error and clears loading without throwing', () => {
    const query = TestBed.runInInjectionContext(() =>
      createQuerySignal<void, string>(() => throwError(() => new Error('boom'))),
    );

    query.load(undefined);

    expect(query.loading()).toBe(false);
    expect(query.error()).toBeInstanceOf(Error);
    expect(query.data()).toBeNull();
  });
});
