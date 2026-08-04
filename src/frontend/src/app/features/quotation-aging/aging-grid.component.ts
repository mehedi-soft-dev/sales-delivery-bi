import { Component, input, output } from '@angular/core';
import { TableModule } from 'primeng/table';
import type { AgedQuotationDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { StatusBadge } from '../../shared/components/status-badge/status-badge';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';

@Component({
  selector: 'app-aging-grid',
  imports: [TableModule, StatusBadge, CurrencyUsdPipe, DaysOpenPipe],
  templateUrl: './aging-grid.component.html',
  styleUrl: './aging-grid.component.css',
})
export class AgingGridComponent {
  readonly page = input.required<PagedResult<AgedQuotationDto>>();
  readonly loading = input(false);
  readonly lazyLoad = output<TableLazyLoadEvent>();
}
