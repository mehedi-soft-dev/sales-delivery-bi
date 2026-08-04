import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { MessageService } from 'primeng/api';
import type { AdminRoleDto } from '../../../core/models/dashboard.models';
import { PermissionsService } from '../permissions/permissions.service';
import { RolesService } from './roles.service';

@Component({
  selector: 'app-role-permissions-dialog',
  imports: [FormsModule, Button, Checkbox, Dialog],
  templateUrl: './role-permissions-dialog.component.html',
  styleUrl: './role-permissions-dialog.component.css',
})
export class RolePermissionsDialogComponent {
  private readonly permissionsService = inject(PermissionsService);
  private readonly rolesService = inject(RolesService);
  private readonly messageService = inject(MessageService);

  readonly role = input<AdminRoleDto | null>(null);
  readonly visible = input(false);
  readonly visibleChange = output<boolean>();
  readonly saved = output<AdminRoleDto>();

  protected readonly allCodes = signal<string[]>([]);
  protected readonly selectedCodes = signal<string[]>([]);
  protected readonly saving = signal(false);

  constructor() {
    effect(() => {
      const role = this.role();
      if (this.visible() && role) {
        this.selectedCodes.set([...role.permissionCodes]);
        this.permissionsService.listAllCodes().subscribe((codes) => this.allCodes.set(codes));
      }
    });
  }

  protected onHide(): void {
    this.visibleChange.emit(false);
  }

  protected onSave(): void {
    const role = this.role();
    if (!role) {
      return;
    }

    this.saving.set(true);
    this.rolesService.updateRolePermissions(role.roleId, this.selectedCodes()).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: 'Permissions updated', detail: `Saved for ${updated.roleName}.` });
        this.saved.emit(updated);
        this.visibleChange.emit(false);
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Save failed', detail: 'Could not update role permissions. Please try again.' });
      },
    });
  }
}
