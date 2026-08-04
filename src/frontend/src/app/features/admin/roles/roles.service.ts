import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import type { AdminRoleDto, PagedResult } from '../../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../../shared/data/grid-query';
import { createPagedQuerySignal } from '../../../shared/data/paged-query-signal';

const ROLES_ENDPOINT = `${environment.apiBaseUrl}/admin/roles`;

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);

  private readonly query = createPagedQuerySignal<GridQuery, AdminRoleDto>((grid) =>
    this.http.get<PagedResult<AdminRoleDto>>(ROLES_ENDPOINT, { params: appendGridParams(new HttpParams(), grid) }),
  );

  readonly page = this.query.page;
  readonly loading = this.query.loading;
  readonly error = this.query.error;

  load(grid: GridQuery): void {
    this.query.load(grid);
  }
}
