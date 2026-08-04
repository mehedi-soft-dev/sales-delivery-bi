import { Component, effect, inject, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../../shared/data/grid-query';
import { UsersService } from './users.service';

@Component({
  selector: 'app-users-page',
  imports: [PageHeader, TableModule],
  templateUrl: './users.page.html',
  styleUrl: './users.page.css',
})
export class UsersPage {
  protected readonly service = inject(UsersService);
  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  constructor() {
    effect(() => this.service.load(this.grid()));
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }
}
