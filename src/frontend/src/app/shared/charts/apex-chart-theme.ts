import type { ApexChart, ApexOptions } from 'ng-apexcharts';

type BaseChartOptions = Omit<Partial<ApexOptions>, 'chart'> & { chart: Partial<ApexChart> };

/**
 * Palette source: validated categorical/status/ordinal palette (dataviz skill,
 * `references/palette.md`), confirmed with `validate_palette.js` — light mode only,
 * this app has no dark-mode requirement. Won/lost is a real good/bad pair, so it
 * wears status tokens (good/critical), not arbitrary categorical slots — same rule
 * that keeps status colors out of ordinary "series 4" charts.
 */
export const CHART_COLORS = {
  trend: '#2a78d6',
  statusGood: '#0ca30c',
  statusCritical: '#d03b3b',
  statusSerious: '#ec835a',
  statusWarning: '#fab219',
  agingOrdinalRamp: ['#86b6ef', '#5598e7', '#2a78d6', '#1c5cab', '#104281'],
} as const;

export const CHART_INK = {
  primary: '#0b0b0b',
  secondary: '#52514e',
  muted: '#898781',
  gridline: '#e1e0d9',
  baseline: '#c3c2b7',
} as const;

export const CHART_FONT_FAMILY = 'system-ui, -apple-system, "Segoe UI", sans-serif';

const CHART_RESPONSIVE_BREAKPOINTS: ApexOptions['responsive'] = [
  {
    breakpoint: 768,
    options: {
      chart: { height: 260 },
      legend: { position: 'bottom' },
    },
  },
  {
    breakpoint: 1024,
    options: {
      chart: { height: 300 },
    },
  },
];

/** Base options every chart component spreads and extends with its own `series`/`colors`/`chart.type`. */
export function createBaseChartOptions(): BaseChartOptions {
  return {
    chart: {
      fontFamily: CHART_FONT_FAMILY,
      foreColor: CHART_INK.secondary,
      toolbar: { show: false },
      background: 'transparent',
    },
    grid: {
      borderColor: CHART_INK.gridline,
    },
    xaxis: {
      axisBorder: { color: CHART_INK.baseline },
      axisTicks: { color: CHART_INK.baseline },
      labels: { style: { colors: CHART_INK.muted } },
    },
    yaxis: {
      labels: { style: { colors: CHART_INK.muted } },
    },
    tooltip: {
      theme: 'light',
    },
    dataLabels: { enabled: false },
    responsive: CHART_RESPONSIVE_BREAKPOINTS,
  };
}
