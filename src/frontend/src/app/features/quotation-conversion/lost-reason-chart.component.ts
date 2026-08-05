import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexPlotOptions, ApexTooltip, ApexXAxis } from 'ng-apexcharts';
import type { LostReasonBreakdownDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** Win/Loss reason analysis (docs/requirements §4.2, secondary view) — horizontal bar, value (USD) per lost reason. */
@Component({
  selector: 'app-lost-reason-chart',
  imports: [ChartComponent],
  templateUrl: './lost-reason-chart.component.html',
})
export class LostReasonChartComponent {
  readonly reasons = input.required<LostReasonBreakdownDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'bar', height: 260 };
  protected readonly colors = [CHART_COLORS.statusCritical];
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = { enabled: false };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { horizontal: true, borderRadius: 4, barHeight: '55%' },
  };
  protected readonly legend = { show: false };
  protected readonly responsive = this.base.responsive;
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: { formatter: (value: number) => formatUsd(value) },
  };

  private readonly orderedReasons = computed(() => [...this.reasons()].sort((a, b) => normalizeNumber(b.valueUsd) - normalizeNumber(a.valueUsd)));

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.orderedReasons().map((entry) => entry.reason),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Lost Value (USD)',
      data: this.orderedReasons().map((entry) => normalizeNumber(entry.valueUsd)),
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
