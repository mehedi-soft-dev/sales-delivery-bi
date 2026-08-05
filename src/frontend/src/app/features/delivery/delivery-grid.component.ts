import { Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { TableModule } from 'primeng/table';
import type { DeliveryRowDto, PagedResult } from '../../core/models/dashboard.models';
import type { TableLazyLoadEvent } from '../../shared/data/grid-query';
import { CurrencyUsdPipe } from '../../shared/pipes/currency-usd.pipe';

@Component({
  selector: 'app-delivery-grid',
  imports: [TableModule, NgClass, CurrencyUsdPipe],
  templateUrl: './delivery-grid.component.html',
  styleUrl: './delivery-grid.component.css',
})
export class DeliveryGridComponent {
  readonly page = input.required<PagedResult<DeliveryRowDto>>();
  readonly loading = input(false);
  readonly sortField = input<string | null>(null);
  readonly sortOrder = input(1);
  readonly lazyLoad = output<TableLazyLoadEvent>();

  protected rowLateClass(deliveryStatus: string): Record<string, boolean> {
    return { 'delivery-grid__row--late': deliveryStatus === 'Late' };
  }
}
