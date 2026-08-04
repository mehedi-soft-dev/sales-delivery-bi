import { Component, effect, inject, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../../shared/data/grid-query';
import { PermissionsService } from './permissions.service';

@Component({
  selector: 'app-permissions-page',
  imports: [PageHeader, TableModule],
  templateUrl: './permissions.page.html',
  styleUrl: './permissions.page.css',
})
export class PermissionsPage {
  protected readonly service = inject(PermissionsService);
  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  constructor() {
    effect(() => this.service.load(this.grid()));
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }
}
