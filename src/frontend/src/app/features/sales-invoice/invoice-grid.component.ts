import { Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { TableModule } from 'primeng/table';
import type { InvoiceRowDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';

@Component({
  selector: 'app-invoice-grid',
  imports: [TableModule, NgClass, CurrencyUsdPipe],
  templateUrl: './invoice-grid.component.html',
  styleUrl: './invoice-grid.component.css',
})
export class InvoiceGridComponent {
  readonly page = input.required<PagedResult<InvoiceRowDto>>();
  readonly loading = input(false);
  readonly sortField = input<string | null>(null);
  readonly sortOrder = input(1);
  readonly lazyLoad = output<TableLazyLoadEvent>();

  protected rowOverdueClass(arStatus: string): Record<string, boolean> {
    return { 'invoice-grid__row--overdue': arStatus === 'Overdue' };
  }
}
