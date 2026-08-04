import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { CurrentUserService } from './current-user.service';

describe('AuthService', () => {
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    authService = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('posts credentials to the login endpoint and stores the returned token', () => {
    let result: { token: string; displayName: string } | undefined;
    authService.login('user@example.com', 'secret').subscribe((response) => (result = response));

    const req = httpTesting.expectOne(`${environment.apiBaseUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'secret' });

    req.flush({ token: 'the-token', expiresAtUtc: '2026-01-01T00:00:00Z', displayName: 'Test User' });

    expect(result?.token).toBe('the-token');
    expect(TestBed.inject(CurrentUserService).token()).toBe('the-token');
  });

  it('propagates the error and never stores a token on a failed login', () => {
    let errored = false;
    authService.login('user@example.com', 'wrong').subscribe({ error: () => (errored = true) });

    httpTesting
      .expectOne(`${environment.apiBaseUrl}/auth/login`)
      .flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBe(true);
    expect(TestBed.inject(CurrentUserService).token()).toBeNull();
  });
});
