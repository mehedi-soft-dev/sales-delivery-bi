import { Component, effect, inject, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../../shared/data/grid-query';
import { RolesService } from './roles.service';

@Component({
  selector: 'app-roles-page',
  imports: [PageHeader, TableModule],
  templateUrl: './roles.page.html',
  styleUrl: './roles.page.css',
})
export class RolesPage {
  protected readonly service = inject(RolesService);
  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  constructor() {
    effect(() => this.service.load(this.grid()));
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }
}
