import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, QuotationPipelineResponseDto } from '../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface PipelineFilter {
  unitId: string | null;
  includeDraft: boolean;
  grid: GridQuery;
}

const PIPELINE_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/pipeline`;

@Injectable({ providedIn: 'root' })
export class PipelineService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<PipelineFilter, QuotationPipelineResponseDto>((filter) => {
    let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
    params = params.set('includeDraft', filter.includeDraft);
    params = appendGridParams(params, filter.grid);
    return this.http.get<DashboardResponse<QuotationPipelineResponseDto>>(PIPELINE_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: PipelineFilter): void {
    this.query.load(filter);
  }
}
