import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from '../auth/current-user.service';
import { fakeJwt } from '../auth/testing/fake-jwt';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('attaches the Authorization header to API requests when a token is present', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) + 3600 });
    TestBed.inject(CurrentUserService).setToken(token);

    httpClient.get(`${environment.apiBaseUrl}/sales/quotations/pipeline`).subscribe();

    const req = httpTesting.expectOne(`${environment.apiBaseUrl}/sales/quotations/pipeline`);
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
    req.flush({});
  });

  it('does not attach a header to API requests when there is no token', () => {
    httpClient.get(`${environment.apiBaseUrl}/sales/quotations/pipeline`).subscribe();

    const req = httpTesting.expectOne(`${environment.apiBaseUrl}/sales/quotations/pipeline`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('strips/never attaches the Authorization header on non-API requests', () => {
    const token = fakeJwt({ sub: 'user-1', exp: Math.floor(Date.now() / 1000) + 3600 });
    TestBed.inject(CurrentUserService).setToken(token);

    httpClient.get('/assets/logo.png').subscribe();

    const req = httpTesting.expectOne('/assets/logo.png');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
