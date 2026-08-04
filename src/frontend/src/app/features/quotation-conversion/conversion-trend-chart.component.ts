import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import type { MonthlyTrendEntryDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

@Component({
  selector: 'app-conversion-trend-chart',
  imports: [ChartComponent],
  templateUrl: './conversion-trend-chart.component.html',
})
export class ConversionTrendChartComponent {
  readonly trend = input.required<MonthlyTrendEntryDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'line', height: 280 };
  protected readonly colors = [CHART_COLORS.trend];
  protected readonly stroke = { curve: 'smooth' as const, width: 3 };
  protected readonly grid = this.base.grid;
  // Enabled (overriding the shared default) so the conversion rate is readable without hovering —
  // this dashboard has no accompanying data table, so the chart is the only place to read it.
  protected readonly dataLabels: ApexDataLabels = {
    enabled: true,
    formatter: (value: number | string) => `${Math.round(Number(value))}%`,
    style: { colors: [CHART_COLORS.trend] },
    offsetY: -8,
  };
  protected readonly responsive = this.base.responsive;
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: { formatter: (value: number) => `${Math.round(value)}%` },
  };
  protected readonly yaxis: ApexYAxis = {
    max: 100,
    labels: { formatter: (value: number) => `${Math.round(value)}%` },
  };

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Conversion Rate',
      data: this.trend().map((entry) => normalizeNumber(entry.conversionRatePct)),
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
