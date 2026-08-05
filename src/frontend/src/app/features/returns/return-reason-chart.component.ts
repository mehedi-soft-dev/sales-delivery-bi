import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexPlotOptions, ApexTooltip, ApexXAxis } from 'ng-apexcharts';
import type { ReturnReasonBreakdownDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** Top return reasons — same shape as LostReasonChartComponent (horizontal bar, value USD per reason). */
@Component({
  selector: 'app-return-reason-chart',
  imports: [ChartComponent],
  templateUrl: './return-reason-chart.component.html',
})
export class ReturnReasonChartComponent {
  readonly reasons = input.required<ReturnReasonBreakdownDto[]>();

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

  private readonly orderedReasons = computed(() =>
    [...this.reasons()].sort((a, b) => normalizeNumber(b.valueUsd) - normalizeNumber(a.valueUsd)),
  );

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.orderedReasons().map((entry) => entry.reasonCode),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Return Value (USD)',
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
