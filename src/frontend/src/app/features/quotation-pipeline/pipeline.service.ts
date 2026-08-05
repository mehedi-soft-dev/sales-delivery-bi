import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { DashboardResponse, QuotationPipelineResponseDto } from '../../core/models/dashboard.models';
import { DEFAULT_GRID_QUERY, appendGridParams, type GridQuery } from '../../shared/data/grid-query';
import { createQuerySignal } from '../../shared/data/query-signal';

export interface PipelineFilter {
  unitId: string | null;
  includeDraft: boolean;
  status: string | null;
  buyerName: string | null;
  grid: GridQuery;
}

const PIPELINE_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/pipeline`;
const PIPELINE_EXPORT_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/pipeline/export`;

function buildParams(filter: PipelineFilter): HttpParams {
  let params = filter.unitId ? new HttpParams().set('unitId', filter.unitId) : new HttpParams();
  params = params.set('includeDraft', filter.includeDraft);
  if (filter.status) {
    params = params.set('status', filter.status);
  }
  if (filter.buyerName) {
    params = params.set('buyerName', filter.buyerName);
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class PipelineService {
  private readonly http = inject(HttpClient);

  private readonly query = createQuerySignal<PipelineFilter, QuotationPipelineResponseDto>((filter) => {
    const params = appendGridParams(buildParams(filter), filter.grid);
    return this.http.get<DashboardResponse<QuotationPipelineResponseDto>>(PIPELINE_ENDPOINT, { params });
  });

  readonly data = this.query.data;
  readonly lastRefresh = this.query.lastRefresh;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(filter: PipelineFilter): void {
    this.query.load(filter);
  }

  exportToExcel(filter: Omit<PipelineFilter, 'grid'>): Observable<Blob> {
    return this.http.get(PIPELINE_EXPORT_ENDPOINT, { params: buildParams({ ...filter, grid: DEFAULT_GRID_QUERY }), responseType: 'blob' });
  }
}
