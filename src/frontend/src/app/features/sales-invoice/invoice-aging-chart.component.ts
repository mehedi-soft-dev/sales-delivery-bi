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
import type { InvoiceAgingBucketDto } from '../../core/models/dashboard.models';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

/** Fixed display order, same reasoning as AgingBucketChartComponent — the API doesn't guarantee array order. */
const BUCKET_ORDER = ['Current', '1-30', '31-60', '60+'];

/** Combo chart: outstanding value (bars, left axis) + invoice count (line, right axis) per AR aging bucket. */
@Component({
  selector: 'app-invoice-aging-chart',
  imports: [ChartComponent],
  templateUrl: './invoice-aging-chart.component.html',
})
export class InvoiceAgingChartComponent {
  readonly agingBuckets = input.required<InvoiceAgingBucketDto[]>();

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
      title: { text: 'Outstanding (USD)' },
      labels: { formatter: (value: number) => formatUsd(value) },
    },
    {
      opposite: true,
      title: { text: 'Invoices' },
      labels: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
      forceNiceScale: true,
    },
  ];

  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: [{ formatter: (value: number) => formatUsd(value) }, { formatter: (value: number) => Math.round(value).toLocaleString('en-US') }],
  };

  private readonly orderedBuckets = computed(() => {
    const byBucket = new Map(this.agingBuckets().map((entry) => [entry.bucket, entry]));
    return BUCKET_ORDER.map((bucket) => byBucket.get(bucket) ?? { bucket, count: 0, valueUsd: 0 });
  });

  protected readonly xaxis = computed<ApexXAxis>(() => ({
    ...this.base.xaxis,
    categories: this.orderedBuckets().map((entry) => entry.bucket),
  }));

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Outstanding (USD)',
      type: 'column',
      data: this.orderedBuckets().map((entry) => normalizeNumber(entry.valueUsd)),
    },
    {
      name: 'Invoices',
      type: 'line',
      data: this.orderedBuckets().map((entry) => normalizeNumber(entry.count)),
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
