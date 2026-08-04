import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexLegend, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import type { MonthlyTrendEntryDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** Two lines per month: Won Count vs Lost Count. */
@Component({
  selector: 'app-conversion-trend-chart',
  imports: [ChartComponent],
  templateUrl: './conversion-trend-chart.component.html',
})
export class ConversionTrendChartComponent {
  readonly trend = input.required<MonthlyTrendEntryDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'line', height: 280 };
  protected readonly colors = [CHART_COLORS.statusGood, CHART_COLORS.statusCritical];
  protected readonly stroke = { curve: 'smooth' as const, width: 3 };
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

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Won',
      data: this.trend().map((entry) => normalizeNumber(entry.wonCount)),
    },
    {
      name: 'Lost',
      data: this.trend().map((entry) => normalizeNumber(entry.lostCount)),
    },
  ]);

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.trend().map((entry) => entry.month),
  }));
}

function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}
