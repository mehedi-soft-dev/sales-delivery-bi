import { Component, HostListener, computed, input, output, signal } from '@angular/core';
import { Avatar } from 'primeng/avatar';
import { Toolbar } from 'primeng/toolbar';
import { Bars } from '@primeicons/angular/bars';
import { Building } from '@primeicons/angular/building';
import { User } from '@primeicons/angular/user';

@Component({
  selector: 'app-topbar',
  imports: [Toolbar, Avatar, Bars, Building, User],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css',
})
export class Topbar {
  readonly unitLabel = input('All Units');
  readonly userDisplayName = input<string | null>(null);

  readonly menuToggle = output<void>();
  readonly logout = output<void>();

  protected readonly userMenuOpen = signal(false);

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

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.userMenuOpen.set(!this.userMenuOpen());
  }

  onLogoutClick(): void {
    this.userMenuOpen.set(false);
    this.logout.emit();
  }

  @HostListener('document:click')
  @HostListener('document:keydown.escape')
  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }
}
