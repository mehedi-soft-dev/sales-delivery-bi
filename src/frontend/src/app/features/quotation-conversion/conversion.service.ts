import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { ConversionResponseDto, DashboardResponse } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface ConversionFilter {
  unitId: string | null;
  fromDate: string | null;
  toDate: string | null;
  grid: GridQuery;
}

const CONVERSION_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/conversion`;

@Injectable({ providedIn: 'root' })
export class ConversionService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<ConversionFilter, ConversionResponseDto>((filter) => {
    let params = new HttpParams();
    if (filter.unitId) {
      params = params.set('unitId', filter.unitId);
    }
    if (filter.fromDate) {
      params = params.set('fromDate', filter.fromDate);
    }
    if (filter.toDate) {
      params = params.set('toDate', filter.toDate);
    }
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<ConversionResponseDto>>(CONVERSION_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: ConversionFilter): void {
    this.query.load(filter);
  }
}
