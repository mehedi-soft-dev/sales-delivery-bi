import type { DateRangeFilterValue } from '../components/date-range-filter/date-range-filter';

const LAST_30_DAYS_SPAN = 29;

/** Default date range for dashboards that filter by date: today minus 29 days through today (30 days inclusive). */
export function last30DaysRange(): DateRangeFilterValue {
  const today = new Date();
  const fromDate = new Date(today);
  fromDate.setDate(today.getDate() - LAST_30_DAYS_SPAN);

  return { fromDate: toIsoDate(fromDate), toDate: toIsoDate(today) };
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
