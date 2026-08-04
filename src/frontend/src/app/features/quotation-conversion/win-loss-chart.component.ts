import { Component, computed, input } from '@angular/core';
import { ChartComponent } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexPlotOptions, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import { CHART_COLORS, createBaseChartOptions } from '../../shared/charts/apex-chart-theme';

interface ColorPoint {
  readonly seriesIndex: number;
  readonly dataPointIndex: number;
}

const WON_VALUE_COLOR = CHART_COLORS.statusGood;
const WON_COUNT_COLOR = CHART_COLORS.trend;
const LOST_VALUE_COLOR = CHART_COLORS.statusCritical;
const LOST_COUNT_COLOR = CHART_COLORS.statusWarning;

/** dataPointIndex 0 = "Won" category, 1 = "Lost" (xaxis.categories below); seriesIndex 0 = Value, 1 = Count. */
function fourWayColor({ seriesIndex, dataPointIndex }: ColorPoint): string {
  const isWon = dataPointIndex === 0;
  const isValue = seriesIndex === 0;
  if (isWon) {
    return isValue ? WON_VALUE_COLOR : WON_COUNT_COLOR;
  }
  return isValue ? LOST_VALUE_COLOR : LOST_COUNT_COLOR;
}

/**
 * Grouped column chart: Value (USD) + Count, two bars per Won/Lost category — four distinct colors,
 * one per Won-Value/Won-Count/Lost-Value/Lost-Count combination. ApexCharts' built-in legend only
 * has one swatch per series (2), which can't represent 4 categories, so the legend in the template
 * is a plain custom row instead of `legend: {show: true}`.
 */
@Component({
  selector: 'app-win-loss-chart',
  imports: [ChartComponent],
  templateUrl: './win-loss-chart.component.html',
  styleUrl: './win-loss-chart.component.css',
})
export class WinLossChartComponent {
  readonly wonValueUsd = input.required<number | string>();
  readonly lostValueUsd = input.required<number | string>();
  readonly wonCount = input.required<number | string>();
  readonly lostCount = input.required<number | string>();

  private readonly base = createBaseChartOptions();

  protected readonly chart: ApexChart = { ...this.base.chart, type: 'bar', height: 280 };
  protected readonly colors = [fourWayColor, fourWayColor];
  protected readonly grid = this.base.grid;
  protected readonly dataLabels: ApexDataLabels = { enabled: false };
  protected readonly responsive = this.base.responsive;
  protected readonly legend = { show: false };
  protected readonly plotOptions: ApexPlotOptions = {
    bar: { columnWidth: '55%', borderRadius: 4 },
  };
  protected readonly xaxis: ApexXAxis = { ...this.base.xaxis, categories: ['Won', 'Lost'] };
  protected readonly yaxis: ApexYAxis[] = [
    {
      title: { text: 'Value (USD)' },
      labels: { formatter: (value: number) => formatUsd(value) },
    },
    {
      opposite: true,
      title: { text: 'Count' },
      labels: { formatter: (value: number) => Math.round(value).toLocaleString('en-US') },
      forceNiceScale: true,
    },
  ];
  protected readonly tooltip: ApexTooltip = {
    ...this.base.tooltip,
    y: [{ formatter: (value: number) => formatUsd(value) }, { formatter: (value: number) => Math.round(value).toLocaleString('en-US') }],
  };

  protected readonly legendItems = [
    { label: 'Won Value', color: WON_VALUE_COLOR },
    { label: 'Won Count', color: WON_COUNT_COLOR },
    { label: 'Lost Value', color: LOST_VALUE_COLOR },
    { label: 'Lost Count', color: LOST_COUNT_COLOR },
  ];

  protected readonly series = computed<ApexAxisChartSeries>(() => [
    {
      name: 'Value (USD)',
      data: [normalizeNumber(this.wonValueUsd()), normalizeNumber(this.lostValueUsd())],
    },
    {
      name: 'Count',
      data: [normalizeNumber(this.wonCount()), normalizeNumber(this.lostCount())],
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
