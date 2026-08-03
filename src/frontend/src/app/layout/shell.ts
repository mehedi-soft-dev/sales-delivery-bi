import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Topbar } from './topbar/topbar';
import { Sidebar } from './sidebar/sidebar';
import { Footer } from './footer/footer';
import { LoadingIndicatorService } from '../core/http/loading-indicator';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, Topbar, Sidebar, Footer],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  protected readonly loadingIndicator = inject(LoadingIndicatorService);
  protected readonly sidebarOpen = signal(false);
}
