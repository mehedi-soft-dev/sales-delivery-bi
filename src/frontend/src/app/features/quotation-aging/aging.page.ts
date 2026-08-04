import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { IncludeDraftToggle } from '../../shared/components/include-draft-toggle/include-draft-toggle';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { AgingBucketChartComponent } from './aging-bucket-chart.component';
import { AgingGridComponent } from './aging-grid.component';
import { AgingService } from './aging.service';

@Component({
  selector: 'app-aging-page',
  imports: [DataAsOf, KpiCard, LoadingSkeleton, PageHeader, IncludeDraftToggle, AgingBucketChartComponent, AgingGridComponent, CurrencyUsdPipe],
  templateUrl: './aging.page.html',
  styleUrl: './aging.page.css',
})
export class AgingPage {
  protected readonly service = inject(AgingService);
  private readonly unitFilterStore = inject(UnitFilterStore);

  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);
  protected readonly includeDraft = signal(false);

  constructor() {
    effect(() => {
      this.service.load({ unitId: this.unitFilterStore.unitId(), includeDraft: this.includeDraft(), grid: this.grid() });
    });
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onRefresh(): void {
    this.service.load({ unitId: this.unitFilterStore.unitId(), includeDraft: this.includeDraft(), grid: this.grid() });
  }
}
