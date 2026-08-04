import { TestBed } from '@angular/core/testing';
import { DataAsOf } from './data-as-of';

describe('DataAsOf', () => {
  it('renders the formatted lastRefresh value', () => {
    const fixture = TestBed.createComponent(DataAsOf);
    fixture.componentRef.setInput('lastRefresh', '2026-08-03T09:12:00Z');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Data as of');
    expect(text).toMatch(/2026/);
  });

  it('renders a neutral placeholder when lastRefresh is null (never a hardcoded date)', () => {
    const fixture = TestBed.createComponent(DataAsOf);
    fixture.componentRef.setInput('lastRefresh', null);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text.trim()).toBe('Data as of —');
  });
});
