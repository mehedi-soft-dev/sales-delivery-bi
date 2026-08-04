import { TestBed } from '@angular/core/testing';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  it('renders the status label for every known status', () => {
    const statuses = [
      'Draft',
      'Submitted',
      'Negotiation',
      'PendingApproval',
      'Approved',
      'Converted',
      'Rejected',
      'Expired',
    ];

    for (const status of statuses) {
      const fixture = TestBed.createComponent(StatusBadge);
      fixture.componentRef.setInput('status', status);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.status-badge__label')?.textContent?.trim()).toBe(status);
      expect(el.querySelector('svg[data-p-icon]')).toBeTruthy();
    }
  });

  it('falls back to a default icon/color for an unrecognized status', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'SomethingNew');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('svg[data-p-icon="circle"]')).toBeTruthy();
  });
});
