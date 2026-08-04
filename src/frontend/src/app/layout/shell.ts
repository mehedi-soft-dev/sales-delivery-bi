import { Component, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { Topbar } from './topbar/topbar';
import { Sidebar } from './sidebar/sidebar';
import { Footer } from './footer/footer';
import { LoadingIndicatorService } from '../core/http/loading-indicator';
import { CurrentUserService } from '../core/auth/current-user.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, Topbar, Sidebar, Footer],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  protected readonly loadingIndicator = inject(LoadingIndicatorService);
  protected readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);

  protected readonly sidebarOpen = signal(false);
  /** Desktop-only icon rail collapse, toggled from the topbar. */
  protected readonly sidebarCollapsed = signal(false);

  onLogout(): void {
    this.currentUser.clearToken();
    void this.router.navigateByUrl('/login');
  }
}
