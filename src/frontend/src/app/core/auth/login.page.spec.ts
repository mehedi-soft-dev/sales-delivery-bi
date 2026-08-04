import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { environment } from '../../../environments/environment';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  let httpTesting: HttpTestingController;
  let navigateByUrl: (url: string) => Promise<boolean>;

  beforeEach(() => {
    localStorage.clear();
    navigateByUrl = vi.fn().mockResolvedValue(true) as unknown as (url: string) => Promise<boolean>;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: Router, useValue: { navigateByUrl } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('navigates to /pipeline on a successful login', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance;
    (component as unknown as { email: string }).email = 'user@example.com';
    (component as unknown as { password: string }).password = 'secret';

    component.onSubmit();

    httpTesting
      .expectOne(`${environment.apiBaseUrl}/auth/login`)
      .flush({ token: 'the-token', expiresAtUtc: '2026-01-01T00:00:00Z', displayName: 'Test User' });

    expect(navigateByUrl).toHaveBeenCalledWith('/pipeline');
  });

  it('shows an inline error and never navigates on a failed login', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance;
    (component as unknown as { email: string }).email = 'user@example.com';
    (component as unknown as { password: string }).password = 'wrong';

    component.onSubmit();

    httpTesting
      .expectOne(`${environment.apiBaseUrl}/auth/login`)
      .flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(navigateByUrl).not.toHaveBeenCalled();
    expect((component as unknown as { errorMessage: () => string | null }).errorMessage()).toContain(
      'Invalid email or password',
    );
  });

  it('does nothing when submitted with an empty field', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance;
    (component as unknown as { email: string }).email = '';
    (component as unknown as { password: string }).password = '';

    component.onSubmit();

    httpTesting.expectNone(`${environment.apiBaseUrl}/auth/login`);
  });
});
