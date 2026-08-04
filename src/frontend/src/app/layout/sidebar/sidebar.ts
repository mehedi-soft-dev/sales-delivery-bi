import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, model, output } from '@angular/core';
import { Drawer } from 'primeng/drawer';
import { ChartBar } from '@primeicons/angular/chart-bar';
import { CurrentUserService } from '../../core/auth/current-user.service';
import { NAV_TREE, type NavNode } from '../nav-items';
import { NavNodeComponent } from './nav-node';

@Component({
  selector: 'app-sidebar',
  imports: [NgTemplateOutlet, Drawer, ChartBar, NavNodeComponent],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  private readonly currentUser = inject(CurrentUserService);

  readonly mobileOpen = model(false);
  /** Desktop-only rail collapse (icon-only), toggled from the topbar. */
  readonly collapsed = input(false);
  /** Asks the shell to un-collapse the desktop rail — bubbled up from a nav-node opened while collapsed. */
  readonly expandRequest = output<void>();

  protected readonly visibleNav = computed<readonly NavNode[]>(() =>
    NAV_TREE.filter((node) => node.kind !== 'group' || !node.permission || this.currentUser.hasPermission(node.permission)),
  );

  onLeafClick(): void {
    this.mobileOpen.set(false);
  }
}
