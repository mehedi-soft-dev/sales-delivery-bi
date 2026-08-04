import { Component, input, output } from '@angular/core';
import { TableModule } from 'primeng/table';
import type { OpenQuotationDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { StatusBadge } from '../../shared/components/status-badge/status-badge';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';

@Component({
  selector: 'app-pipeline-grid',
  imports: [TableModule, StatusBadge, CurrencyUsdPipe, DaysOpenPipe],
  templateUrl: './pipeline-grid.component.html',
  styleUrl: './pipeline-grid.component.css',
})
export class PipelineGridComponent {
  readonly page = input.required<PagedResult<OpenQuotationDto>>();
  readonly loading = input(false);
  readonly lazyLoad = output<TableLazyLoadEvent>();
}
