import { CurrencyPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

const USD_DISPLAY_FORMAT = '1.0-0';

/**
 * Wraps CurrencyPipe with a fixed USD format and normalizes the API's
 * `number | string` decimal fields (System.Text.Json's OpenAPI schema for
 * C# `decimal`) into a plain number before formatting.
 */
@Pipe({ name: 'currencyUsd' })
export class CurrencyUsdPipe implements PipeTransform {
  private readonly currencyPipe = new CurrencyPipe('en-US');

  transform(value: number | string | null | undefined): string {
    if (value === null || value === undefined) {
      return '—';
    }

    const numeric = typeof value === 'string' ? Number(value) : value;
    if (Number.isNaN(numeric)) {
      return '—';
    }

    return this.currencyPipe.transform(numeric, 'USD', 'symbol', USD_DISPLAY_FORMAT) ?? '—';
  }
}
