import { Component, ElementRef, HostListener, computed, inject, input, output, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';
import { Avatar } from 'primeng/avatar';
import { Toolbar } from 'primeng/toolbar';
import { Popover } from 'primeng/popover';
import { Select } from 'primeng/select';
import { Bars } from '@primeicons/angular/bars';
import { Building } from '@primeicons/angular/building';
import { User } from '@primeicons/angular/user';
import { Search } from '@primeicons/angular/search';
import { Bell } from '@primeicons/angular/bell';
import { ChevronDown } from '@primeicons/angular/chevron-down';
import { SignOut } from '@primeicons/angular/sign-out';
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
import { UnitFilterStore } from '../../core/filters/unit-filter.store';
import { findActiveBreadcrumb } from '../nav-items';

@Component({
  selector: 'app-topbar',
  imports: [
    FormsModule,
    Toolbar,
    Avatar,
    Popover,
    Select,
    Bars,
    Building,
    User,
    Search,
    Bell,
    ChevronDown,
    SignOut,
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
  ],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css',
})
export class Topbar {
  private readonly router = inject(Router);
  protected readonly unitFilterStore = inject(UnitFilterStore);

  readonly userDisplayName = input<string | null>(null);
  readonly userRole = input<string | null>(null);
  /** Whether the desktop rail is currently collapsed — drives the collapse-toggle button's pressed state. */
  readonly sidebarCollapsed = input(false);

  readonly menuToggle = output<void>();
  readonly collapseToggle = output<void>();
  readonly logout = output<void>();

  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  protected readonly breadcrumb = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => findActiveBreadcrumb(this.router.url)),
    ),
    { initialValue: findActiveBreadcrumb(this.router.url) },
  );

  readonly userInitials = computed(() => {
    const name = this.userDisplayName();
    if (!name) {
      return null;
    }
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('');
  });

  onLogoutClick(): void {
    this.logout.emit();
  }

  @HostListener('document:keydown', ['$event'])
  protected onGlobalKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.searchInput()?.nativeElement.focus();
    }
  }
}
