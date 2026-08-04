import { HttpParams } from '@angular/common/http';

/** Mirrors the backend's Application/Common/GridQuery.cs — bound as query params on every grid endpoint. */
export interface GridQuery {
  page: number;
  pageSize: number;
  sortField: string | null;
  sortDescending: boolean;
}

export const DEFAULT_GRID_QUERY: GridQuery = { page: 1, pageSize: 10, sortField: null, sortDescending: false };

export function appendGridParams(params: HttpParams, grid: GridQuery): HttpParams {
  let result = params.set('page', grid.page).set('pageSize', grid.pageSize);
  if (grid.sortField) {
    result = result.set('sortField', grid.sortField).set('sortDescending', grid.sortDescending);
  }
  return result;
}

/** Minimal shape of PrimeNG's `p-table` `(onLazyLoad)` event — avoids a hard dependency on primeng/table's type here. */
export interface TableLazyLoadEvent {
  first?: number;
  rows?: number;
  sortField?: string | string[] | null;
  sortOrder?: number | null;
}

export function gridQueryFromLazyLoadEvent(event: TableLazyLoadEvent): GridQuery {
  const pageSize = event.rows ?? DEFAULT_GRID_QUERY.pageSize;
  const first = event.first ?? 0;
  const sortField = Array.isArray(event.sortField) ? (event.sortField[0] ?? null) : (event.sortField ?? null);

  return {
    page: Math.floor(first / pageSize) + 1,
    pageSize,
    sortField,
    sortDescending: (event.sortOrder ?? 1) < 0,
  };
}
