import { Injectable, computed, inject, signal } from '@angular/core';
import { CurrentUserService } from '../auth/current-user.service';
import { resolveUnitDisplayName } from './dev-unit-catalog';

export interface UnitOption {
  readonly id: string | null;
  readonly label: string;
}

const ALL_UNITS_OPTION: UnitOption = { id: null, label: 'All Units' };

/**
 * The single source of truth for the globally-selected unit filter — owned by the topbar's
 * "All Units" dropdown (shell.html), read reactively by every dashboard page via an `effect()`.
 * Options come from the caller's own JWT `user_units` claim only, never a full unit catalog —
 * a user can never even see, let alone select, a unit outside their row-level assignment.
 */
@Injectable({ providedIn: 'root' })
export class UnitFilterStore {
  private readonly currentUser = inject(CurrentUserService);

  readonly unitId = signal<string | null>(null);

  readonly unitOptions = computed<UnitOption[]>(() => [
    ALL_UNITS_OPTION,
    ...this.currentUser.userUnits().map((id) => ({ id, label: resolveUnitDisplayName(id) })),
  ]);

  readonly selectedLabel = computed(() => this.unitOptions().find((option) => option.id === this.unitId())?.label ?? 'All Units');

  setUnitId(unitId: string | null): void {
    this.unitId.set(unitId);
  }
}
