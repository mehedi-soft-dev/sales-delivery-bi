import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexLegend,
  ApexPlotOptions,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
} from 'ng-apexcharts';
import type { StatusFunnelEntryDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';
import { formatStatusLabel } from '../../shared/components/status-badge/status-badge.config';

/** Grouped column chart: quotation count + value (USD), two bars per pipeline status. */
@Component({
  selector: 'app-status-value-chart',
  imports: [ChartComponent],
  templateUrl: './status-value-chart.component.html',
})
export class StatusValueChartComponent {
  readonly entries = input.required<readonly StatusFunnelEntryDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'bar', height: 260 };
  protected readonly colors = [CHART_COLORS.statusWarning, CHART_COLORS.trend];
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = { enabled: false };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { columnWidth: '55%', borderRadius: 4 },
  };
  protected readonly legend: ApexLegend = { show: true, position: 'top', horizontalAlign: 'right' };
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: [{ formatter: (value: number) => Math.round(value).toLocaleString('en-US') }, { formatter: (value: number) => formatUsd(value) }],
  };
  protected readonly responsive = this.base.responsive;

  protected readonly yaxis: ApexYAxis[] = [
    {
      title: { text: 'Quotations' },
      labels: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
      forceNiceScale: true,
    },
    {
      opposite: true,
      title: { text: 'Value (USD)' },
      labels: { formatter: (value: number) => formatUsd(value) },
    },
  ];

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.entries().map((entry) => formatStatusLabel(entry.status)),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Quotations',
      data: this.entries().map((entry) => normalizeNumber(entry.count)),
    },
    {
      name: 'Value (USD)',
      data: this.entries().map((entry) => normalizeNumber(entry.valueUsd)),
    },
  ]);
}

/** Counts/values arrive as `number | string` (System.Text.Json decimal/int quirk) — normalize before charting. */
function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}

function formatUsd(value: number): string {
  return `$${Math.round(value).toLocaleString('en-US')}`;
}
