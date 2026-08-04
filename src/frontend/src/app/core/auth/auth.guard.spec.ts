import { TestBed } from '@angular/core/testing';
import { UrlTree, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { CurrentUserService } from './current-user.service';
import { fakeJwt } from './testing/fake-jwt';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
  });

  it('allows navigation when authenticated', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) + 3600 });
    TestBed.inject(CurrentUserService).setToken(token);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('redirects to /login with a returnUrl when not authenticated', () => {
    const state = { url: '/pipeline' } as never;
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login?returnUrl=%2Fpipeline');
  });
});
