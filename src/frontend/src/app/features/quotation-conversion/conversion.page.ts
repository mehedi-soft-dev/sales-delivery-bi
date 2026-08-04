import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { DateRangeFilter, type DateRangeFilterValue } from '../../shared/components/date-range-filter/date-range-filter';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { BuyerPerformanceGridComponent } from './buyer-performance-grid.component';
import { ConversionTrendChartComponent } from './conversion-trend-chart.component';
import { WinLossChartComponent } from './win-loss-chart.component';
import { ConversionService } from './conversion.service';

@Component({
  selector: 'app-conversion-page',
  imports: [
    DataAsOf,
    DateRangeFilter,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    BuyerPerformanceGridComponent,
    ConversionTrendChartComponent,
    WinLossChartComponent,
    CurrencyUsdPipe,
    DaysOpenPipe,
    RatePercentPipe,
  ],
  templateUrl: './conversion.page.html',
  styleUrl: './conversion.page.css',
})
export class ConversionPage {
  protected readonly service = inject(ConversionService);
  private readonly unitFilterStore = inject(UnitFilterStore);

  protected readonly dateRange = signal<DateRangeFilterValue>({ fromDate: null, toDate: null });
  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  constructor() {
    effect(() => {
      const { fromDate, toDate } = this.dateRange();
      this.service.load({ unitId: this.unitFilterStore.unitId(), fromDate, toDate, grid: this.grid() });
    });
  }

  protected onDateRangeChange(value: DateRangeFilterValue): void {
    this.dateRange.set(value);
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onRefresh(): void {
    const { fromDate, toDate } = this.dateRange();
    this.service.load({ unitId: this.unitFilterStore.unitId(), fromDate, toDate, grid: this.grid() });
  }
}
