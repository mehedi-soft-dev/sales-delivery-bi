import { Component, computed, input } from '@angular/core';
import { Pencil } from '@primeicons/angular/pencil';
import { Send } from '@primeicons/angular/send';
import { Comments } from '@primeicons/angular/comments';
import { Hourglass } from '@primeicons/angular/hourglass';
import { Check } from '@primeicons/angular/check';
import { CheckCircle } from '@primeicons/angular/check-circle';
import { TimesCircle } from '@primeicons/angular/times-circle';
import { CalendarTimes } from '@primeicons/angular/calendar-times';
import { Circle } from '@primeicons/angular/circle';
import { DEFAULT_STATUS_BADGE_COLOR, STATUS_BADGE_COLOR } from './status-badge.config';

@Component({
  selector: 'app-status-badge',
  imports: [Pencil, Send, Comments, Hourglass, Check, CheckCircle, TimesCircle, CalendarTimes, Circle],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.css',
})
export class StatusBadge {
  readonly status = input.required<string>();

  readonly color = computed(() => STATUS_BADGE_COLOR[this.status()] ?? DEFAULT_STATUS_BADGE_COLOR);
}
