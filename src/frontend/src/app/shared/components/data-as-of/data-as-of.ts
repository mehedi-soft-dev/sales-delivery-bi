import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { History } from '@primeicons/angular/history';
import { Refresh } from '@primeicons/angular/refresh';

@Component({
  selector: 'app-data-as-of',
  imports: [DatePipe, History, Refresh],
  templateUrl: './data-as-of.html',
  styleUrl: './data-as-of.css',
})
export class DataAsOf {
  /** Bind directly to the feature service's `lastRefresh` signal — never hardcode or compute this client-side. */
  readonly lastRefresh = input.required<string | null>();
  /** Emits when the refresh button is clicked — re-runs the same load the page did on init/filter-change. */
  readonly refresh = output<void>();
}
