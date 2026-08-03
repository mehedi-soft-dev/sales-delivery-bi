import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Mock, vi } from 'vitest';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;
  let navigateByUrl: Mock;
  let messageAdd: Mock;

  beforeEach(() => {
    navigateByUrl = vi.fn();
    messageAdd = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigateByUrl } },
        { provide: MessageService, useValue: { add: messageAdd } },
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('redirects to /403 and never shows a toast on a 403 response', () => {
    httpClient.get('/api/sales/quotations/pipeline').subscribe({ error: () => undefined });

    httpTesting.expectOne('/api/sales/quotations/pipeline').flush('Forbidden', { status: 403, statusText: 'Forbidden' });

    expect(navigateByUrl).toHaveBeenCalledWith('/403');
    expect(messageAdd).not.toHaveBeenCalled();
  });

  it('shows a toast (never redirects) on a 404 response', () => {
    httpClient.get('/api/sales/quotations/999').subscribe({ error: () => undefined });

    httpTesting.expectOne('/api/sales/quotations/999').flush('Not Found', { status: 404, statusText: 'Not Found' });

    expect(messageAdd).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows a toast (never redirects) on a 500 response', () => {
    httpClient.get('/api/sales/quotations/pipeline').subscribe({ error: () => undefined });

    httpTesting
      .expectOne('/api/sales/quotations/pipeline')
      .flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    expect(messageAdd).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});
