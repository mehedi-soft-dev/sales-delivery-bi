import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, QuotationSummaryDto } from '../../core/models/dashboard.models';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface ExecutiveOverviewFilter {
  unitId: string | null;
  /** Scopes Conversion Rate only — Open Pipeline Value/High-Value Aged Alerts are always "right now" snapshots. */
  fromDate: string | null;
  toDate: string | null;
}

const SUMMARY_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/summary`;

@Injectable({ providedIn: 'root' })
export class ExecutiveOverviewService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<ExecutiveOverviewFilter, QuotationSummaryDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    if (filter.fromDate) {
      params = params.set('fromDate', filter.fromDate);
    }
    if (filter.toDate) {
      params = params.set('toDate', filter.toDate);
    }
    return this.http.get<DashboardResponse<QuotationSummaryDto>>(SUMMARY_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: ExecutiveOverviewFilter): void {
    this.query.load(filter);
  }
}
