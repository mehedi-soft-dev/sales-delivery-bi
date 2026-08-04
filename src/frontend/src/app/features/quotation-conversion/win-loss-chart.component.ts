import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexPlotOptions, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/**
 * The API only exposes an aggregate won/lost value for the filtered period (`ConversionKpisDto`),
 * not a per-month breakdown — so this is a 2-bar Won-vs-Lost comparison, not a "by month" trend.
 */
@Component({
  selector: 'app-win-loss-chart',
  imports: [ChartComponent],
  templateUrl: './win-loss-chart.component.html',
})
export class WinLossChartComponent {
  readonly wonValueUsd = input.required<number | string>();
  readonly lostValueUsd = input.required<number | string>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'bar', height: 280 };
  protected readonly colors = [CHART_COLORS.statusGood, CHART_COLORS.statusCritical];
  protected readonly grid = this.base.grid;
  // Enabled (overriding the shared default) so Won/Lost values are readable without hovering.
  protected readonly dataLabels: ApexDataLabels = {
    enabled: true,
    formatter: (value: number | string) => formatUsd(Number(value)),
    style: { colors: ['#fff'] },
  };
  protected readonly responsive = this.base.responsive;
  protected readonly legend = { show: false };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { distributed: true, borderRadius: 4, columnWidth: '45%' },
  };
  protected readonly xaxis: ApexXAxis = { ...this.base.xaxis, categories: ['Won', 'Lost'] };
  protected readonly yaxis: ApexYAxis = {
    labels: { formatter: (value: number) => formatUsd(value) },
  };
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: { formatter: (value: number) => formatUsd(value) },
  };

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Value (USD)',
      data: [normalizeNumber(this.wonValueUsd()), normalizeNumber(this.lostValueUsd())],
    },
  ]);
}

function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}

function formatUsd(value: number): string {
  return `$${Math.round(value).toLocaleString('en-US')}`;
}
