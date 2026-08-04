import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from '../auth/current-user.service';
import type { UnitOptionDto } from '../models/dashboard.models';

export interface UnitOption {
  readonly id: string | null;
  readonly label: string;
}

const ALL_UNITS_OPTION: UnitOption = { id: null, label: 'All Units' };
const UNITS_ENDPOINT = `${environment.apiBaseUrl}/sales/quotations/units`;

/**
 * The single source of truth for the globally-selected unit filter — owned by the topbar's
 * "All Units" dropdown (shell.html), read reactively by every dashboard page via an `effect()`.
 * Options come from GET /sales/quotations/units, which is itself scoped server-side by
 * IUnitAccessGuard — a caller with bi.quotation.viewAllUnits gets every unit in the DB, everyone
 * else gets only their own assigned units. Never a client-side unit catalog.
 *
 * Refetches whenever the authenticated user (CurrentUserService.sub) changes — this store is a
 * root singleton that outlives any single login, so without this a login → logout → different-user
 * login cycle (no full page reload in between) would keep showing the PREVIOUS user's unit list.
 */
@Injectable({ providedIn: 'root' })
export class UnitFilterStore {
  private readonly http = inject(HttpClient);
  private readonly currentUser = inject(CurrentUserService);

  private readonly accessibleUnits = signal<UnitOptionDto[]>([]);

  readonly unitId = signal<string | null>(null);

  readonly unitOptions = computed<UnitOption[]>(() => [
    ALL_UNITS_OPTION,
    ...this.accessibleUnits().map((unit) => ({ id: unit.id, label: unit.name })),
  ]);

  readonly selectedLabel = computed(() => this.unitOptions().find((option) => option.id === this.unitId())?.label ?? 'All Units');

  constructor() {
    effect(() => {
      const sub = this.currentUser.sub();
      this.unitId.set(null);

      if (!sub) {
        this.accessibleUnits.set([]);
        return;
      }

      this.http.get<UnitOptionDto[]>(UNITS_ENDPOINT).subscribe((units) => this.accessibleUnits.set(units));
    });
  }

  setUnitId(unitId: string | null): void {
    this.unitId.set(unitId);
  }
}
