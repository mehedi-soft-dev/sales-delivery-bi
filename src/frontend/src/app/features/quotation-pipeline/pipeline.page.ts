import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Button } from 'primeng/button';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';
import { DataAsOf } from '../../shared/components/data-as-of/data-as-of';
import { KpiCard } from '../../shared/components/kpi-card/kpi-card';
import { LoadingSkeleton } from '../../shared/components/loading-skeleton/loading-skeleton';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { IncludeDraftToggle } from '../../shared/components/include-draft-toggle/include-draft-toggle';
import { formatStatusLabel } from '../../shared/components/status-badge/status-badge.config';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../shared/data/grid-query';
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { PipelineGridComponent } from './pipeline-grid.component';
import { PipelineService } from './pipeline.service';
import { StatusFunnelChartComponent } from './status-funnel-chart.component';
import { StatusValueChartComponent } from './status-value-chart.component';

@Component({
  selector: 'app-pipeline-page',
  imports: [
    DataAsOf,
    KpiCard,
    LoadingSkeleton,
    PageHeader,
    IncludeDraftToggle,
    Button,
    StatusFunnelChartComponent,
    StatusValueChartComponent,
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
  private readonly route = inject(ActivatedRoute);

  protected readonly grid = signal<GridQuery>({ ...DEFAULT_GRID_QUERY, sortField: 'quotationNo', sortDescending: true });
  protected readonly includeDraft = signal(false);
  protected readonly statusFilter = signal<string | null>(null);
  protected readonly buyerNameFilter = signal<string | null>(null);
  protected readonly exporting = signal(false);

  protected readonly formatStatusLabel = formatStatusLabel;

  constructor() {
    // Conversion's buyer-performance grid drills down here via ?buyerName= (docs/requirements §4.2).
    const buyerNameParam = this.route.snapshot.queryParamMap.get('buyerName');
    if (buyerNameParam) {
      this.buyerNameFilter.set(buyerNameParam);
    }

    effect(() => {
      this.service.load(this.currentFilter());
    });
  }

  protected onStatusSelected(status: string): void {
    this.statusFilter.set(status);
    this.grid.update((g) => ({ ...g, page: 1 }));
  }

  protected clearStatusFilter(): void {
    this.statusFilter.set(null);
    this.grid.update((g) => ({ ...g, page: 1 }));
  }

  protected clearBuyerNameFilter(): void {
    this.buyerNameFilter.set(null);
    this.grid.update((g) => ({ ...g, page: 1 }));
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onRefresh(): void {
    this.service.load(this.currentFilter());
  }

  protected onExport(): void {
    this.exporting.set(true);
    const { grid: _grid, ...exportFilter } = this.currentFilter();
    this.service.exportToExcel(exportFilter).subscribe({
      next: (blob) => {
        this.exporting.set(false);
        downloadBlob(blob, `quotation-pipeline-${new Date().toISOString().slice(0, 10)}.xlsx`);
      },
      error: () => this.exporting.set(false),
    });
  }

  /** KPI counts arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before display. */
  protected asCount(value: number | string): string {
    const numeric = typeof value === 'string' ? Number(value) : value;
    return Number.isNaN(numeric) ? '—' : String(Math.round(numeric));
  }

  private currentFilter() {
    return {
      unitId: this.unitFilterStore.unitId(),
      includeDraft: this.includeDraft(),
      status: this.statusFilter(),
      buyerName: this.buyerNameFilter(),
      grid: this.grid(),
    };
  }
}

function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}
