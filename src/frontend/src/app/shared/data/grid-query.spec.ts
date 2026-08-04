import { HttpParams } from '@angular/common/http';
import { appendGridParams, gridQueryFromLazyLoadEvent } from './grid-query';

describe('appendGridParams', () => {
  it('always sets page/pageSize, and only sets sort params when a sortField is present', () => {
    const withoutSort = appendGridParams(new HttpParams(), { page: 2, pageSize: 20, sortField: null, sortDescending: false });
    expect(withoutSort.toString()).toBe('page=2&pageSize=20');

    const withSort = appendGridParams(new HttpParams(), { page: 1, pageSize: 10, sortField: 'valueUsd', sortDescending: true });
    expect(withSort.toString()).toBe('page=1&pageSize=10&sortField=valueUsd&sortDescending=true');
  });
});

describe('gridQueryFromLazyLoadEvent', () => {
  it('converts a zero-based `first` offset into a 1-based page number', () => {
    expect(gridQueryFromLazyLoadEvent({ first: 0, rows: 10 })).toEqual({
      page: 1,
      pageSize: 10,
      sortField: null,
      sortDescending: false,
    });

    expect(gridQueryFromLazyLoadEvent({ first: 20, rows: 10 })).toEqual({
      page: 3,
      pageSize: 10,
      sortField: null,
      sortDescending: false,
    });
  });

  it('maps PrimeNG sortOrder (1 | -1) to sortDescending, and unwraps an array sortField', () => {
    expect(gridQueryFromLazyLoadEvent({ first: 0, rows: 10, sortField: 'buyerName', sortOrder: -1 })).toEqual({
      page: 1,
      pageSize: 10,
      sortField: 'buyerName',
      sortDescending: true,
    });

    expect(gridQueryFromLazyLoadEvent({ first: 0, rows: 10, sortField: ['status'], sortOrder: 1 })).toEqual({
      page: 1,
      pageSize: 10,
      sortField: 'status',
      sortDescending: false,
    });
  });
});
