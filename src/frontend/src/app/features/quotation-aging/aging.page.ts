import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import type { RiskLevelBucketDto } from '../../core/models/dashboard.models';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { IncludeDraftToggle } from '../../shared/components/include-draft-toggle/include-draft-toggle';
import { HighRiskOnlyToggle } from '../../shared/components/high-risk-only-toggle/high-risk-only-toggle';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { AgingBucketChartComponent } from './aging-bucket-chart.component';
import { AgingGridComponent } from './aging-grid.component';
import { AgingService } from './aging.service';

@Component({
  selector: 'app-aging-page',
  imports: [
    DataAsOf,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    IncludeDraftToggle,
    HighRiskOnlyToggle,
    AgingBucketChartComponent,
    AgingGridComponent,
    CurrencyUsdPipe,
  ],
  templateUrl: './aging.page.html',
  styleUrl: './aging.page.css',
})
export class AgingPage {
  protected readonly service = inject(AgingService);
  private readonly unitFilterStore = inject(UnitFilterStore);

  protected readonly grid = signal<GridQuery>({ ...DEFAULT_GRID_QUERY, sortField: 'daysOpen', sortDescending: true });
  protected readonly includeDraft = signal(false);
  protected readonly highRiskOnly = signal(false);

  constructor() {
    effect(() => {
      this.service.load({
        unitId: this.unitFilterStore.unitId(),
        includeDraft: this.includeDraft(),
        highRiskOnly: this.highRiskOnly(),
        grid: this.grid(),
      });
    });
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onRefresh(): void {
    this.service.load({
      unitId: this.unitFilterStore.unitId(),
      includeDraft: this.includeDraft(),
      highRiskOnly: this.highRiskOnly(),
      grid: this.grid(),
    });
  }

  /** Counts arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before display. */
  protected asCount(value: number | string): string {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? '—' : String(Math.round(numeric));
  }

  protected highRiskCountOf(riskLevels: readonly RiskLevelBucketDto[]): number {
    const entry = riskLevels.find((level) => level.riskLevel === 'High');
    if (!entry) {
      return 0;
    }
    const numeric = typeof entry.count === 'string' ? Number(entry.count) : entry.count;
    return Number.isNaN(numeric) ? 0 : numeric;
  }
}
