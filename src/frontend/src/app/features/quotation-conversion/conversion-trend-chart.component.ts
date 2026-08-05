import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexLegend, ApexStroke, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import type { MonthlyTrendEntryDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/**
 * Two solid lines per month (Won/Lost Count, current period) plus two dashed "previous period" comparison
 * lines (docs/requirements §4.2's "trend comparison, current vs previous period") — the previous-period
 * series is aligned by relative month position (1st, 2nd, ...), not by calendar label, since it covers a
 * different, earlier set of calendar months than the x-axis categories (which always show the current period).
 */
@Component({
  selector: 'app-conversion-trend-chart',
  imports: [ChartComponent],
  templateUrl: './conversion-trend-chart.component.html',
})
export class ConversionTrendChartComponent {
  readonly trend = input.required<MonthlyTrendEntryDto[]>();
  readonly previousTrend = input<MonthlyTrendEntryDto[]>([]);

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'line', height: 280 };
  protected readonly colors = [CHART_COLORS.statusGood, CHART_COLORS.statusCritical, CHART_COLORS.statusGood, CHART_COLORS.statusCritical];
  protected readonly stroke: ApexStroke = { curve: 'smooth', width: [3, 3, 2, 2], dashArray: [0, 0, 5, 5] };
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = { enabled: false };
  protected readonly legend: ApexLegend = { show: true, position: 'top', horizontalAlign: 'right' };
  protected readonly responsive = this.base.responsive;
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
  };
  protected readonly yaxis: ApexYAxis = {
    forceNiceScale: true,
    labels: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
  };

  protected readonly series = computed<ApexAxisChartSeries>(() => {
    const previous = this.previousTrend();
    return [
      { name: 'Won', data: this.trend().map((entry) => normalizeNumber(entry.wonCount)) },
      { name: 'Lost', data: this.trend().map((entry) => normalizeNumber(entry.lostCount)) },
      { name: 'Won (Previous Period)', data: previous.map((entry) => normalizeNumber(entry.wonCount)) },
      { name: 'Lost (Previous Period)', data: previous.map((entry) => normalizeNumber(entry.lostCount)) },
    ];
  });

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.trend().map((entry) => entry.month),
  }));
}

function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}
