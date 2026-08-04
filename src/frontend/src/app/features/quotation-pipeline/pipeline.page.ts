import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { PipelineGridComponent } from './pipeline-grid.component';
import { PipelineService } from './pipeline.service';
import { StatusFunnelChartComponent } from './status-funnel-chart.component';

@Component({
  selector: 'app-pipeline-page',
  imports: [
    DataAsOf,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    StatusFunnelChartComponent,
    PipelineGridComponent,
    CurrencyUsdPipe,
    DaysOpenPipe,
  ],
  templateUrl: './pipeline.page.html',
  styleUrl: './pipeline.page.css',
})
export class PipelinePage {
  protected readonly service = inject(PipelineService);
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

  /** KPI counts arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before display. */
  protected asCount(value: number | string): string {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? '—' : String(Math.round(numeric));
  }
}
