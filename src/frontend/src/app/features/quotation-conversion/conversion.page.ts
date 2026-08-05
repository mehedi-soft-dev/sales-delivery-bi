import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { DateRangeFilter, type DateRangeFilterValue } from '../../shared/components/date-range-filter/date-range-filter';
import { last30DaysRange } from '../../shared/data/date-range-defaults';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { BuyerPerformanceGridComponent } from './buyer-performance-grid.component';
import { ConversionTrendChartComponent } from './conversion-trend-chart.component';
import { LostReasonChartComponent } from './lost-reason-chart.component';
import { WinLossChartComponent } from './win-loss-chart.component';
import { ConversionService } from './conversion.service';

@Component({
  selector: 'app-conversion-page',
  imports: [
    DataAsOf,
    DatePipe,
    DateRangeFilter,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    BuyerPerformanceGridComponent,
    ConversionTrendChartComponent,
    LostReasonChartComponent,
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
  private readonly router = inject(Router);

  protected readonly defaultDateRange = last30DaysRange();
  protected readonly dateRange = signal<DateRangeFilterValue>(this.defaultDateRange);
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

  /** Drill down to that buyer's open quotations on the Pipeline dashboard (docs/requirements §4.2). */
  protected onBuyerSelected(buyerName: string): void {
    void this.router.navigate(['/pipeline'], { queryParams: { buyerName } });
  }
}
