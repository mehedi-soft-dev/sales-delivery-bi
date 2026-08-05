import { Component, input, output } from '@angular/core';
import { TableModule } from 'primeng/table';
import type { PagedResult, ReturnRowDto } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';

@Component({
  selector: 'app-returns-grid',
  imports: [TableModule, CurrencyUsdPipe],
  templateUrl: './returns-grid.component.html',
  styleUrl: './returns-grid.component.css',
})
export class ReturnsGridComponent {
  readonly page = input.required<PagedResult<ReturnRowDto>>();
  readonly loading = input(false);
  readonly sortField = input<string | null>(null);
  readonly sortOrder = input(1);
  readonly lazyLoad = output<TableLazyLoadEvent>();
}
