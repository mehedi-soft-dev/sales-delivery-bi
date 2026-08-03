import { TestBed } from '@angular/core/testing';
import { CurrentUserService } from './current-user.service';
import { fakeJwt } from './testing/fake-jwt';

describe('CurrentUserService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('starts unauthenticated with no stored token', () => {
    const service = TestBed.inject(CurrentUserService);
    expect(service.isAuthenticated()).toBe(false);
    expect(service.sub()).toBeNull();
    expect(service.permissions()).toEqual([]);
    expect(service.userUnits()).toEqual([]);
  });

  it('exposes sub/permissions/user_units from a valid, unexpired token', () => {
    const token = fakeJwt({
      sub: 'user-1',
      permissions: ['bi.quotation.view'],
      user_units: ['unit-1'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    const service = TestBed.inject(CurrentUserService);
    service.setToken(token);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.sub()).toBe('user-1');
    expect(service.permissions()).toEqual(['bi.quotation.view']);
    expect(service.userUnits()).toEqual(['unit-1']);
    expect(service.hasPermission('bi.quotation.view')).toBe(true);
    expect(service.hasPermission('bi.quotation.viewAllUnits')).toBe(false);
  });

  it('treats an expired token as unauthenticated', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) - 60 });

    const service = TestBed.inject(CurrentUserService);
    service.setToken(token);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('reads a previously stored token back on construction', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) + 3600 });
    localStorage.setItem('sdbi_auth_token', token);

    const service = TestBed.inject(CurrentUserService);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.sub()).toBe('user-1');
  });

  it('clears the token on logout', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) + 3600 });
    const service = TestBed.inject(CurrentUserService);
    service.setToken(token);

    service.clearToken();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.token()).toBeNull();
  });
});
