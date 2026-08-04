import { TestBed } from '@angular/core/testing';
import { LoadingSkeleton } from './loading-skeleton';

describe('LoadingSkeleton', () => {
  it('renders the default 4 KPI placeholders and 1 content block', () => {
    const fixture = TestBed.createComponent(LoadingSkeleton);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.loading-skeleton__kpi').length).toBe(4);
    expect(el.querySelectorAll('.loading-skeleton > p-skeleton').length).toBe(1);
  });

  it('renders a custom kpiCount and multiple content blocks', () => {
    const fixture = TestBed.createComponent(LoadingSkeleton);
    fixture.componentRef.setInput('kpiCount', 2);
    fixture.componentRef.setInput('contentBlockHeights', [300, 400]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.loading-skeleton__kpi').length).toBe(2);
    expect(el.querySelectorAll('.loading-skeleton > p-skeleton').length).toBe(2);
  });
});
