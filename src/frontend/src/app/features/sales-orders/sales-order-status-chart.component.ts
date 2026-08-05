import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexLegend,
  ApexPlotOptions,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
} from 'ng-apexcharts';
import type { SalesOrderStatusBucketDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** Combo chart: backlog value (bars, left axis) + order count (line, right axis) per status — same shape as AgingBucketChartComponent. */
@Component({
  selector: 'app-sales-order-status-chart',
  imports: [ChartComponent],
  templateUrl: './sales-order-status-chart.component.html',
})
export class SalesOrderStatusChartComponent {
  readonly statusBreakdown = input.required<SalesOrderStatusBucketDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'line', height: 300 };
  protected readonly colors = [CHART_COLORS.trend, CHART_COLORS.statusWarning];
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = { enabled: false };
  protected readonly stroke: ApexStroke = { width: [0, 2], curve: 'smooth' };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { borderRadius: 4, columnWidth: '28%' },
  };
  protected readonly legend: ApexLegend = { show: true, position: 'top', horizontalAlign: 'right' };
  protected readonly responsive = this.base.responsive;

  protected readonly yaxis: ApexYAxis[] = [
    {
      title: { text: 'Value (USD)' },
      labels: { formatter: (value: number) => formatUsd(value) },
    },
    {
      opposite: true,
      title: { text: 'Orders' },
      labels: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
      forceNiceScale: true,
    },
  ];

  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: [{ formatter: (value: number) => formatUsd(value) }, { formatter: (value: number) => Math.round(value).toLocaleString('en-US') }],
  };

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.statusBreakdown().map((entry) => entry.status),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Value (USD)',
      type: 'column',
      data: this.statusBreakdown().map((entry) => normalizeNumber(entry.valueUsd)),
    },
    {
      name: 'Orders',
      type: 'line',
      data: this.statusBreakdown().map((entry) => normalizeNumber(entry.count)),
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
