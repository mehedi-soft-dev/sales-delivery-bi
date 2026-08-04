import { Component, effect, inject, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { DEFAULT_GRID_QUERY, gridQueryFromLazyLoadEvent, type GridQuery, type TableLazyLoadEvent } from '../../../shared/data/grid-query';
import { CurrentUserService } from '../../../core/auth/current-user.service';
import { PermissionCodes } from '../../../core/auth/permission-codes';
import type { AdminRoleDto } from '../../../core/models/dashboard.models';
import { RolePermissionsDialogComponent } from './role-permissions-dialog.component';
import { RolesService } from './roles.service';

@Component({
  selector: 'app-roles-page',
  imports: [PageHeader, TableModule, Button, RolePermissionsDialogComponent],
  templateUrl: './roles.page.html',
  styleUrl: './roles.page.css',
})
export class RolesPage {
  protected readonly service = inject(RolesService);
  private readonly currentUser = inject(CurrentUserService);
  protected readonly grid = signal<GridQuery>(DEFAULT_GRID_QUERY);

  protected readonly canManagePermissions = this.currentUser.hasPermission(PermissionCodes.AdminManage);
  protected readonly editingRole = signal<AdminRoleDto | null>(null);
  protected readonly dialogVisible = signal(false);

  constructor() {
    effect(() => this.service.load(this.grid()));
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.grid.set(gridQueryFromLazyLoadEvent(event));
  }

  protected onEditPermissions(role: AdminRoleDto): void {
    this.editingRole.set(role);
    this.dialogVisible.set(true);
  }

  protected onDialogSaved(): void {
    this.service.load(this.grid());
  }
}
