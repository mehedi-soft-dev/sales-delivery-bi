import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { DeliveryStatusChartComponent } from './delivery-status-chart.component';
import { DeliveryGridComponent } from './delivery-grid.component';
import { DeliveryService } from './delivery.service';

@Component({
  selector: 'app-delivery-page',
  imports: [
    DataAsOf,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    DeliveryStatusChartComponent,
    DeliveryGridComponent,
    CurrencyUsdPipe,
    RatePercentPipe,
  ],
  templateUrl: './delivery.page.html',
  styleUrl: './delivery.page.css',
})
export class DeliveryPage {
  protected readonly service = inject(DeliveryService);
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

  protected asCount(value: number | string): string {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? '—' : String(Math.round(numeric));
  }
}
