import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, InvoiceResponseDto } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface SalesInvoiceFilter {
  unitId: string | null;
  grid: GridQuery;
}

const SALES_INVOICE_ENDPOINT = `${environment.apiBaseUrl}/sales/invoices/summary`;

@Injectable({ providedIn: 'root' })
export class SalesInvoiceService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<SalesInvoiceFilter, InvoiceResponseDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<InvoiceResponseDto>>(SALES_INVOICE_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: SalesInvoiceFilter): void {
    this.query.load(filter);
  }
}
