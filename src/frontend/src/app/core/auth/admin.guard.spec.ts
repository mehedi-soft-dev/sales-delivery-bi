import { TestBed } from '@angular/core/testing';
import { UrlTree, provideRouter } from '@angular/router';
import { adminGuard } from './admin.guard';
import { CurrentUserService } from './current-user.service';
import { fakeJwt } from './testing/fake-jwt';

describe('adminGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
  });

  it('allows navigation when the caller has the admin.access.view permission', () => {
    const token = fakeJwt({
      sub: 'user-1',
      permissions: ['admin.access.view'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    TestBed.inject(CurrentUserService).setToken(token);

    const result = TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('redirects to /403 when the caller lacks the admin.access.view permission', () => {
    const token = fakeJwt({ sub: 'user-1', permissions: [], exp: Math.floor(Date.now() / 1000) + 3600 });
    TestBed.inject(CurrentUserService).setToken(token);

    const result = TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never));

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/403');
  });
});
