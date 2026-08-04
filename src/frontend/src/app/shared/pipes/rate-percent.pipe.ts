import { Pipe, PipeTransform } from '@angular/core';

/** Normalizes the API's `number | string` decimal fields into a rounded "N%" label. */
@Pipe({ name: 'ratePercent' })
export class RatePercentPipe implements PipeTransform {
  transform(value: number | string | null | undefined): string {
    if (value === null || value === undefined) {
      return '—';
    }

    const numeric = typeof value === 'string' ? Number(value) : value;
    if (Number.isNaN(numeric)) {
      return '—';
    }

    return `${Math.round(numeric)}%`;
  }
}
