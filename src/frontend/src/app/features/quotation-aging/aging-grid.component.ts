import { Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { TableModule } from 'primeng/table';
import type { AgedQuotationDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { StatusBadge } from '../../shared/components/status-badge/status-badge';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';
import { DaysOpenPipe } from '../../shared/pipes/days-open.pipe';

@Component({
  selector: 'app-aging-grid',
  imports: [TableModule, NgClass, StatusBadge, CurrencyUsdPipe, DaysOpenPipe],
  templateUrl: './aging-grid.component.html',
  styleUrl: './aging-grid.component.css',
})
export class AgingGridComponent {
  readonly page = input.required<PagedResult<AgedQuotationDto>>();
  readonly loading = input(false);
  readonly sortField = input<string | null>(null);
  readonly sortOrder = input(1);
  readonly lazyLoad = output<TableLazyLoadEvent>();

  /** Row tint by risk — a secondary signal only; the Risk Level column text is the primary one. */
  protected rowRiskClass(riskLevel: string): Record<string, boolean> {
    return {
      'aging-grid__row--high-risk': riskLevel === 'High',
      'aging-grid__row--medium-risk': riskLevel === 'Medium',
    };
  }
}
