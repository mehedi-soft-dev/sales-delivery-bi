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
import type { DeliveryStatusBucketDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** On-Time vs Late — same good/bad status-token rule as win/loss (real good/bad pair, not a plain categorical slot). */
const STATUS_ORDER = ['On-Time', 'Late'];

@Component({
  selector: 'app-delivery-status-chart',
  imports: [ChartComponent],
  templateUrl: './delivery-status-chart.component.html',
})
export class DeliveryStatusChartComponent {
  readonly statusBreakdown = input.required<DeliveryStatusBucketDto[]>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'bar', height: 280 };
  protected readonly colors = [CHART_COLORS.statusGood, CHART_COLORS.statusCritical];
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = {
    enabled: true,
    formatter: (value: number) => formatUsd(value),
  };
  protected readonly stroke: ApexStroke = { width: 0 };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { borderRadius: 4, columnWidth: '40%', distributed: true },
  };
  protected readonly legend: ApexLegend = { show: false };
  protected readonly responsive = this.base.responsive;

  protected readonly yaxis: ApexYAxis = {
    title: { text: 'Delivered Value (USD)' },
    labels: { formatter: (value: number) => formatUsd(value) },
  };

  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: { formatter: (value: number) => formatUsd(value) },
  };

  private readonly orderedBuckets = computed(() => {
    const byStatus = new Map(this.statusBreakdown().map((entry) => [entry.deliveryStatus, entry]));
    return STATUS_ORDER.map((status) => byStatus.get(status) ?? { deliveryStatus: status, count: 0, valueUsd: 0 });
  });

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.orderedBuckets().map((entry) => entry.deliveryStatus),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    { name: 'Delivered Value (USD)', data: this.orderedBuckets().map((entry) => normalizeNumber(entry.valueUsd)) },
  ]);
}

function normalizeNumber(value: number | string): number {
  const numeric = typeof value === 'string' ? Number(value) : value;
  return Number.isNaN(numeric) ? 0 : numeric;
}

function formatUsd(value: number): string {
  return `$${Math.round(value).toLocaleString('en-US')}`;
}
