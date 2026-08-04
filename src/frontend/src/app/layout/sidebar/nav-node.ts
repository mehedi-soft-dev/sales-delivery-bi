import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Tooltip } from 'primeng/tooltip';
import { Home } from '@primeicons/angular/home';
import { Folder } from '@primeicons/angular/folder';
import { ChartLine } from '@primeicons/angular/chart-line';
import { Percentage } from '@primeicons/angular/percentage';
import { Clock } from '@primeicons/angular/clock';
import { ShoppingCart } from '@primeicons/angular/shopping-cart';
import { Truck } from '@primeicons/angular/truck';
import { Receipt } from '@primeicons/angular/receipt';
import { Reply } from '@primeicons/angular/reply';
import { ChartPie } from '@primeicons/angular/chart-pie';
import { Shield } from '@primeicons/angular/shield';
import { Users } from '@primeicons/angular/users';
import { Sitemap } from '@primeicons/angular/sitemap';
import { Key } from '@primeicons/angular/key';
import { ChevronDown } from '@primeicons/angular/chevron-down';
import { containsActiveRoute, type NavNode } from '../nav-items';

@Component({
  selector: 'app-nav-node',
  imports: [
    NgTemplateOutlet,
    RouterLink,
    RouterLinkActive,
    Tooltip,
    Home,
    Folder,
    ChartLine,
    Percentage,
    Clock,
    ShoppingCart,
    Truck,
    Receipt,
    Reply,
    ChartPie,
    Shield,
    Users,
    Sitemap,
    Key,
    ChevronDown,
    NavNodeComponent,
  ],
  templateUrl: './nav-node.html',
  styleUrl: './nav-node.css',
})
export class NavNodeComponent implements OnInit {
  private readonly router = inject(Router);

  readonly node = input.required<NavNode>();
  readonly depth = input(0);
  /** True only for the desktop icon-only rail — nested children are never rendered while collapsed. */
  readonly isCollapsed = input(false);

  readonly leafClick = output<void>();
  /** Bubbles up "please un-collapse the rail" — emitted when a group is opened while the rail is collapsed. */
  readonly expandRequest = output<void>();

  /** Real initial value is set in ngOnInit — required inputs aren't readable yet during construction (NG0950). */
  protected readonly expanded = signal(false);

  ngOnInit(): void {
    const current = this.node();
    if (current.kind === 'group' && containsActiveRoute(this.router.url, current)) {
      this.expanded.set(true);
    }
  }

  toggle(): void {
    if (this.isCollapsed()) {
      this.expandRequest.emit();
      this.expanded.set(true);
      return;
    }
    this.expanded.set(!this.expanded());
  }

  onChildLeafClick(): void {
    this.leafClick.emit();
  }

  onChildExpandRequest(): void {
    this.expandRequest.emit();
  }
}
