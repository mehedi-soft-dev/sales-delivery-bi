import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, SalesOrderResponseDto } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface SalesOrdersFilter {
  unitId: string | null;
  grid: GridQuery;
}

const SALES_ORDERS_ENDPOINT = `${environment.apiBaseUrl}/sales/orders/summary`;

@Injectable({ providedIn: 'root' })
export class SalesOrdersService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<SalesOrdersFilter, SalesOrderResponseDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<SalesOrderResponseDto>>(SALES_ORDERS_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: SalesOrdersFilter): void {
    this.query.load(filter);
  }
}
