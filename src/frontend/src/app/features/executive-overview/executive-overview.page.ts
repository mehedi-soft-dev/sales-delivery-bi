import { Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { ExecutiveOverviewService } from './executive-overview.service';

@Component({
  selector: 'app-executive-overview-page',
  imports: [DataAsOf, KpiCard, LoadingSkeleton, PageHeader, RouterLink, CurrencyUsdPipe, RatePercentPipe],
  templateUrl: './executive-overview.page.html',
  styleUrl: './executive-overview.page.css',
})
export class ExecutiveOverviewPage {
  protected readonly service = inject(ExecutiveOverviewService);
  private readonly unitFilterStore = inject(UnitFilterStore);

  constructor() {
    effect(() => {
      this.service.load({ unitId: this.unitFilterStore.unitId() });
    });
  }

  protected onRefresh(): void {
    this.service.load({ unitId: this.unitFilterStore.unitId() });
  }

  /** Alert count arrives as `number | string` (System.Text.Json decimal/int quirk) — normalize before display/branching. */
  protected asCount(value: number | string): number {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? 0 : numeric;
  }
}
