import { Component, input, output } from '@angular/core';
import { TableModule } from 'primeng/table';
import type { BuyerPerformanceDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { RatePercentPipe } from '../../shared/pipes/rate-percent.pipe';

@Component({
  selector: 'app-buyer-performance-grid',
  imports: [TableModule, CurrencyUsdPipe, RatePercentPipe],
  templateUrl: './buyer-performance-grid.component.html',
  styleUrl: './buyer-performance-grid.component.css',
})
export class BuyerPerformanceGridComponent {
  readonly page = input.required<PagedResult<BuyerPerformanceDto>>();
  readonly loading = input(false);
  readonly lazyLoad = output<TableLazyLoadEvent>();
}
