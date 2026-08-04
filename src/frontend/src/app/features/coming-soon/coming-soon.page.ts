import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PageHeader } from '../../shared/components/page-header/page-header';
import type { NavIconName } from '../../layout/nav-items';

/** Reused by every nav entry that's currently menu-only (Sales Orders, Delivery, Invoice, Return, Report) — no page-specific logic. */
@Component({
  selector: 'app-coming-soon-page',
  imports: [PageHeader],
  templateUrl: './coming-soon.page.html',
  styleUrl: './coming-soon.page.css',
})
export class ComingSoonPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly title = (this.route.snapshot.data['title'] as string | undefined) ?? 'Coming Soon';
  protected readonly icon = (this.route.snapshot.data['icon'] as NavIconName | undefined) ?? null;
}
