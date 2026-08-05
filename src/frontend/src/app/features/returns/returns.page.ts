import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { ReturnReasonChartComponent } from './return-reason-chart.component';
import { ReturnsGridComponent } from './returns-grid.component';
import { ReturnsService } from './returns.service';

@Component({
  selector: 'app-returns-page',
  imports: [DataAsOf, KpiCard, LoadingSkeleton, PageHeader, ReturnReasonChartComponent, ReturnsGridComponent, CurrencyUsdPipe, RatePercentPipe],
  templateUrl: './returns.page.html',
  styleUrl: './returns.page.css',
})
export class ReturnsPage {
  protected readonly service = inject(ReturnsService);
  private readonly unitFilterStore = inject(UnitFilterStore);

  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  constructor() {
    effect(() => {
      this.service.load({ unitId: this.unitFilterStore.unitId(), grid: this.grid() });
    });
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onRefresh(): void {
    this.service.load({ unitId: this.unitFilterStore.unitId(), grid: this.grid() });
  }
}
