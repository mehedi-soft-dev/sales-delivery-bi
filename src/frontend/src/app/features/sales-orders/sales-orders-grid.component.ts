import { Component, input, output } from '@angular/core';
import { TableModule } from 'primeng/table';
import type { PagedResult, SalesOrderRowDto } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';

@Component({
  selector: 'app-sales-orders-grid',
  imports: [TableModule, CurrencyUsdPipe],
  templateUrl: './sales-orders-grid.component.html',
  styleUrl: './sales-orders-grid.component.css',
})
export class SalesOrdersGridComponent {
  readonly page = input.required<PagedResult<SalesOrderRowDto>>();
  readonly loading = input(false);
  readonly sortField = input<string | null>(null);
  readonly sortOrder = input(1);
  readonly lazyLoad = output<TableLazyLoadEvent>();
}
