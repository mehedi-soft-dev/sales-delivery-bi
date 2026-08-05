import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, DeliveryResponseDto } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface DeliveryFilter {
  unitId: string | null;
  grid: GridQuery;
}

const DELIVERY_ENDPOINT = `${environment.apiBaseUrl}/sales/deliveries/summary`;

@Injectable({ providedIn: 'root' })
export class DeliveryService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<DeliveryFilter, DeliveryResponseDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<DeliveryResponseDto>>(DELIVERY_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: DeliveryFilter): void {
    this.query.load(filter);
  }
}
