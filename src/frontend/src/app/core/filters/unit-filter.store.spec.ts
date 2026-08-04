import { TestBed } from '@angular/core/testing';
import { CurrentUserService } from '../auth/current-user.service';
import { fakeJwt } from '../auth/testing/fake-jwt';
import { UnitFilterStore } from './unit-filter.store';

describe('UnitFilterStore', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('populates unit options from the caller\'s user_units claim only, plus "All Units"', () => {
    const token = fakeJwt({
      sub: 'user-1',
      user_units: ['11111111-1111-1111-1111-111111111101', '99999999-9999-9999-9999-999999999999'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    TestBed.inject(CurrentUserService).setToken(token);

    const store = TestBed.inject(UnitFilterStore);

    expect(store.unitOptions()).toEqual([
      { id: null, label: 'All Units' },
      { id: '11111111-1111-1111-1111-111111111101', label: 'Unit-1 (Knit)' },
      { id: '99999999-9999-9999-9999-999999999999', label: '99999999-9999-9999-9999-999999999999' },
    ]);
  });

  it('defaults to "All Units" selected, and updates selectedLabel when the unit changes', () => {
    const token = fakeJwt({
      sub: 'user-1',
      user_units: ['11111111-1111-1111-1111-111111111101'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    TestBed.inject(CurrentUserService).setToken(token);

    const store = TestBed.inject(UnitFilterStore);
    expect(store.unitId()).toBeNull();
    expect(store.selectedLabel()).toBe('All Units');

    store.setUnitId('11111111-1111-1111-1111-111111111101');

    expect(store.selectedLabel()).toBe('Unit-1 (Knit)');
  });
});
