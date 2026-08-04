import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, type Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type { AdminPermissionDto, PagedResult } from '../../../core/models/dashboard.models';
import { appendGridParams, type GridQuery } from '../../../shared/data/grid-query';
import { createPagedQuerySignal } from '../../../shared/data/paged-query-signal';

const PERMISSIONS_ENDPOINT = `${environment.apiBaseUrl}/admin/permissions`;

/** Comfortably above the system's known permission-code count (4 today) without ever needing a second page. */
const ALL_CODES_PAGE_SIZE = 200;

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

  /** Every known permission code, for rendering a full checkbox list (e.g. the role-permissions edit dialog). */
  listAllCodes(): Observable<string[]> {
    const params = new HttpParams().set('page', 1).set('pageSize', ALL_CODES_PAGE_SIZE);
    return this.http
      .get<PagedResult<AdminPermissionDto>>(PERMISSIONS_ENDPOINT, { params })
      .pipe(map((result) => result.items.map((p) => p.permissionCode)));
  }
}
