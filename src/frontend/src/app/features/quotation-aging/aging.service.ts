import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { AgingResponseDto, DashboardResponse } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface AgingFilter {
  unitId: string | null;
  grid: GridQuery;
}

const AGING_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/aging`;

@Injectable({ providedIn: 'root' })
export class AgingService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<AgingFilter, AgingResponseDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<AgingResponseDto>>(AGING_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: AgingFilter): void {
    this.query.load(filter);
  }
}
