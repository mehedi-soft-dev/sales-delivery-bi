import { TestBed } from '@angular/core/testing';
import { DateRangeFilter } from './date-range-filter';

describe('DateRangeFilter', () => {
  it('debounces filter changes and emits the latest value once, in ISO date form', async () => {
    const fixture = TestBed.createComponent(DateRangeFilter);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    const emitted: unknown[] = [];
    component.filterChange.subscribe((value) => emitted.push(value));

    (component as unknown as { fromDate: Date | null }).fromDate = new Date(2026, 5, 1);
    component.onFilterFieldChange();
    (component as unknown as { toDate: Date | null }).toDate = new Date(2026, 5, 30);
    component.onFilterFieldChange();

    expect(emitted.length).toBe(0);

    await new Promise((resolve) => setTimeout(resolve, 350));

    expect(emitted).toEqual([{ fromDate: '2026-06-01', toDate: '2026-06-30' }]);
  });
});
