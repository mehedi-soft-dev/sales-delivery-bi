import { Component, input } from '@angular/core';
import { ArrowUp } from '@primeicons/angular/arrow-up';
import { ArrowDown } from '@primeicons/angular/arrow-down';
import { Minus } from '@primeicons/angular/minus';
import { File } from '@primeicons/angular/file';
import { Wallet } from '@primeicons/angular/wallet';
import { Hourglass } from '@primeicons/angular/hourglass';
import { CalendarClock } from '@primeicons/angular/calendar-clock';
import { Percentage } from '@primeicons/angular/percentage';
import { CheckCircle } from '@primeicons/angular/check-circle';
import { TimesCircle } from '@primeicons/angular/times-circle';
import { ExclamationTriangle } from '@primeicons/angular/exclamation-triangle';

export interface KpiTrend {
  direction: 'up' | 'down' | 'flat';
  /** Independent of direction — e.g. a rising "lost value" is `direction: 'up'`, `sentiment: 'bad'`. */
  sentiment: 'good' | 'bad' | 'neutral';
  label: string;
}

export type KpiIconName =
  | 'file'
  | 'wallet'
  | 'hourglass'
  | 'calendar-clock'
  | 'percentage'
  | 'check-circle'
  | 'times-circle'
  | 'exclamation-triangle';

export type KpiAccent = 'blue' | 'green' | 'amber' | 'red';

@Component({
  selector: 'app-kpi-card',
  imports: [
    ArrowUp,
    ArrowDown,
    Minus,
    File,
    Wallet,
    Hourglass,
    CalendarClock,
    Percentage,
    CheckCircle,
    TimesCircle,
    ExclamationTriangle,
  ],
  templateUrl: './kpi-card.html',
  styleUrl: './kpi-card.css',
})
export class KpiCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly trend = input<KpiTrend | null>(null);
  readonly icon = input<KpiIconName | null>(null);
  readonly accent = input<KpiAccent>('blue');
}
