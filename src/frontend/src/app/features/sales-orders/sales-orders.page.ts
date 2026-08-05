import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { SalesOrderStatusChartComponent } from './sales-order-status-chart.component';
import { SalesOrdersGridComponent } from './sales-orders-grid.component';
import { SalesOrdersService } from './sales-orders.service';

@Component({
  selector: 'app-sales-orders-page',
  imports: [
    DataAsOf,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    SalesOrderStatusChartComponent,
    SalesOrdersGridComponent,
    CurrencyUsdPipe,
    DaysOpenPipe,
  ],
  templateUrl: './sales-orders.page.html',
  styleUrl: './sales-orders.page.css',
})
export class SalesOrdersPage {
  protected readonly service = inject(SalesOrdersService);
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

  /** Counts arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before display. */
  protected asCount(value: number | string): string {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? '—' : String(Math.round(numeric));
  }
}
