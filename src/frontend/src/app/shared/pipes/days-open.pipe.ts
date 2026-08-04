import { Pipe, PipeTransform } from '@angular/core';

/** Normalizes the API's `number | string` decimal fields into a "N day(s)" label. */
@Pipe({ name: 'daysOpen' })
export class DaysOpenPipe implements PipeTransform {
  transform(value: number | string | null | undefined): string {
    if (value === null || value === undefined) {
      return '—';
    }

    const numeric = typeof value === 'string' ? Number(value) : value;
    if (Number.isNaN(numeric)) {
      return '—';
    }

    const rounded = Math.round(numeric);
    return `${rounded} ${rounded === 1 ? 'day' : 'days'}`;
  }
}
