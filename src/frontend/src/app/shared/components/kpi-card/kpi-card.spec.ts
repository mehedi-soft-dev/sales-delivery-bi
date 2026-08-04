import { TestBed } from '@angular/core/testing';
import { KpiCard } from './kpi-card';

describe('KpiCard', () => {
  it('renders label and value', () => {
    const fixture = TestBed.createComponent(KpiCard);
    fixture.componentRef.setInput('label', 'Open Quotations');
    fixture.componentRef.setInput('value', '24');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.kpi-card__label')?.textContent?.trim()).toBe('Open Quotations');
    expect(el.querySelector('.kpi-card__value')?.textContent?.trim()).toBe('24');
    expect(el.querySelector('.kpi-card__trend')).toBeNull();
  });

  it('renders a trend with the correct sentiment class regardless of arrow direction', () => {
    const fixture = TestBed.createComponent(KpiCard);
    fixture.componentRef.setInput('label', 'Lost Value');
    fixture.componentRef.setInput('value', '$450,000');
    fixture.componentRef.setInput('trend', { direction: 'up', sentiment: 'bad', label: '+12% vs last month' });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const trend = el.querySelector('.kpi-card__trend');
    expect(trend?.classList.contains('kpi-card__trend--bad')).toBe(true);
    expect(trend?.querySelector('svg[data-p-icon="arrow-up"]')).toBeTruthy();
    expect(trend?.textContent).toContain('+12% vs last month');
  });
});
