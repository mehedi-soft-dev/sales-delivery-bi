import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from '../auth/current-user.service';
import { fakeJwt } from '../auth/testing/fake-jwt';
import { UnitFilterStore } from './unit-filter.store';

describe('UnitFilterStore', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  function loginAs(sub: string): void {
    TestBed.inject(CurrentUserService).setToken(fakeJwt({ sub, exp: Math.floor(Date.now() / 1000) + 3600 }));
  }

  it('populates unit options from GET /sales/quotations/units, plus "All Units"', () => {
    loginAs('user-1');
    const store = TestBed.inject(UnitFilterStore);
    TestBed.flushEffects();

    httpTesting.expectOne(`${environment.apiBaseUrl}/sales/quotations/units`).flush([
      { id: '11111111-1111-1111-1111-111111111101', name: 'Unit-1 (Knit)' },
      { id: '11111111-1111-1111-1111-111111111102', name: 'Unit-2 (Woven)' },
    ]);

    expect(store.unitOptions()).toEqual([
      { id: null, label: 'All Units' },
      { id: '11111111-1111-1111-1111-111111111101', label: 'Unit-1 (Knit)' },
      { id: '11111111-1111-1111-1111-111111111102', label: 'Unit-2 (Woven)' },
    ]);
  });

  it('defaults to "All Units" selected, and updates selectedLabel when the unit changes', () => {
    loginAs('user-1');
    const store = TestBed.inject(UnitFilterStore);
    TestBed.flushEffects();

    httpTesting
      .expectOne(`${environment.apiBaseUrl}/sales/quotations/units`)
      .flush([{ id: '11111111-1111-1111-1111-111111111101', name: 'Unit-1 (Knit)' }]);

    expect(store.unitId()).toBeNull();
    expect(store.selectedLabel()).toBe('All Units');

    store.setUnitId('11111111-1111-1111-1111-111111111101');

    expect(store.selectedLabel()).toBe('Unit-1 (Knit)');
  });

  it('refetches and resets the selection when the authenticated user changes (login → different-user login, no page reload)', () => {
    loginAs('super-admin');
    const store = TestBed.inject(UnitFilterStore);
    TestBed.flushEffects();

    httpTesting.expectOne(`${environment.apiBaseUrl}/sales/quotations/units`).flush([
      { id: '11111111-1111-1111-1111-111111111101', name: 'Unit-1 (Knit)' },
      { id: '11111111-1111-1111-1111-111111111102', name: 'Unit-2 (Woven)' },
      { id: '11111111-1111-1111-1111-111111111103', name: 'Unit-3 (Sweater)' },
    ]);
    store.setUnitId('11111111-1111-1111-1111-111111111102');
    expect(store.unitOptions().length).toBe(4);

    // A different, unit-restricted user logs in — same SPA session, no full page reload.
    loginAs('restricted-user');
    TestBed.flushEffects();

    expect(store.unitId()).toBeNull();
    httpTesting
      .expectOne(`${environment.apiBaseUrl}/sales/quotations/units`)
      .flush([{ id: '11111111-1111-1111-1111-111111111101', name: 'Unit-1 (Knit)' }]);

    expect(store.unitOptions()).toEqual([
      { id: null, label: 'All Units' },
      { id: '11111111-1111-1111-1111-111111111101', label: 'Unit-1 (Knit)' },
    ]);
  });
});
