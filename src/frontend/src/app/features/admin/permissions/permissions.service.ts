import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import type { AdminPermissionDto, PagedResult } from '../../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../../shared/data/grid-query';
import { createPagedQuerySignal } from '../../../shared/data/paged-query-signal';

const PERMISSIONS_ENDPOINT = `${environment.apiBaseUrl}/admin/permissions`;

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly http = inject(HttpClient);

  private readonly query = createPagedQuerySignal<GridQuery, AdminPermissionDto>((grid) =>
    this.http.get<PagedResult<AdminPermissionDto>>(PERMISSIONS_ENDPOINT, { params: appendGridParams(new HttpParams(), grid) }),
  );

  readonly page = this.query.page;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(grid: GridQuery): void {
    this.query.load(grid);
  }
}
