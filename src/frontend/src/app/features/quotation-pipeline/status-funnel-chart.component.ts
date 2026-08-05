import { Component, computed, input, output } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexPlotOptions, ApexTooltip, ApexXAxis } from 'ng-apexcharts';
import type { StatusFunnelEntryDto } from '../../core/models/dashboard.models';
import { createBaseChartOptions } from '../../shared/charts/apex-chart-theme';
import { DEFAULT_STATUS_BADGE_COLOR, STATUS_BADGE_COLOR, formatStatusLabel } from '../../shared/components/status-badge/status-badge.config';

/**
 * Horizontal bar, one segment per pipeline stage — same color mapping as `app-status-badge`.
 * Clicking a segment emits its raw status code so the page can filter the grid below (docs/requirements §4.1).
 */
@Component({
  selector: 'app-status-funnel-chart',
  imports: [ChartComponent],
  templateUrl: './status-funnel-chart.component.html',
})
export class StatusFunnelChartComponent {
  readonly entries = input.required<readonly StatusFunnelEntryDto[]>();
  readonly statusSelected = output<string>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = {
    ...this.base.chart,
    type: 'bar',
    height: 260,
    events: {
      dataPointSelection: (_event: unknown, _chartContext: unknown, config: { dataPointIndex: number }) => {
        const entry = this.entries()[config.dataPointIndex];
        if (entry) {
          this.statusSelected.emit(entry.status);
        }
      },
    },
  };
  protected readonly grid = this.base.grid;
  protected readonly legend = { show: false };
  protected readonly dataLabels: ApexDataLabels = {
    enabled: true,
    style: { colors: ['#ffffff'] },
    dropShadow: { enabled: false },
  };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { horizontal: true, distributed: true, borderRadius: 4, barHeight: '60%' },
  };
  protected readonly tooltip: ApexTooltip = { ...this.base.tooltip };
  protected readonly responsive = this.base.responsive;

  protected readonly colors = computed<string[]>(() =>
    this.entries().map((entry) => STATUS_BADGE_COLOR[entry.status] ?? DEFAULT_STATUS_BADGE_COLOR),
  );

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.entries().map((entry) => formatStatusLabel(entry.status)),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Quotations',
      data: this.entries().map((entry) => normalizeNumber(entry.count)),
    },
  ]);
}

/** Counts arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before charting. */
function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}
