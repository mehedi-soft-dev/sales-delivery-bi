import { Component, OnInit, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime } from 'rxjs';
import { DatePicker } from 'primeng/datepicker';

export interface DateRangeFilterValue {
  fromDate: string | null;
  toDate: string | null;
}

const FILTER_CHANGE_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-date-range-filter',
  imports: [FormsModule, DatePicker],
  templateUrl: './date-range-filter.html',
  styleUrl: './date-range-filter.css',
})
export class DateRangeFilter implements OnInit {
  readonly initialValue = input<DateRangeFilterValue | null>(null);
  readonly filterChange = output<DateRangeFilterValue>();

  protected fromDate: Date | null = null;
  protected toDate: Date | null = null;

  private readonly fieldChanges = new Subject<void>();

  constructor() {
    this.fieldChanges
      .pipe(debounceTime(FILTER_CHANGE_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe(() => this.filterChange.emit(this.buildValue()));
  }

  ngOnInit(): void {
    const initial = this.initialValue();
    this.fromDate = fromIsoDate(initial?.fromDate ?? null);
    this.toDate = fromIsoDate(initial?.toDate ?? null);
  }

  onFilterFieldChange(): void {
    this.fieldChanges.next();
  }

  private buildValue(): DateRangeFilterValue {
    return {
      fromDate: this.fromDate ? toIsoDate(this.fromDate) : null,
      toDate: this.toDate ? toIsoDate(this.toDate) : null,
    };
  }
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function fromIsoDate(isoDate: string | null): Date | null {
  if (!isoDate) {
    return null;
  }
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year, month - 1, day);
}
