import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';

import { routes } from './app.routes';
import { loadingIndicatorInterceptor } from './core/http/loading-indicator';
import { errorInterceptor } from './core/http/error.interceptor';
import { authInterceptor } from './core/http/auth.interceptor';

/** Professional blue accent, replacing Aura's default emerald primary — used for active nav state, primary buttons, focus rings. */
const AppTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#eff6ff',
      100: '#dbeafe',
      200: '#bfdbfe',
      300: '#93c5fd',
      400: '#60a5fa',
      500: '#2563eb',
      600: '#2563eb',
      700: '#1d4ed8',
      800: '#1e40af',
      900: '#1e3a8a',
      950: '#172554',
    },
  },
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([loadingIndicatorInterceptor, errorInterceptor, authInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: AppTheme,
        /** This app has no light/dark toggle (per CLAUDE.md, charts are light-mode-only) — never follow the OS `prefers-color-scheme`. */
        options: { darkModeSelector: false },
      },
    }),
    MessageService,
  ]
};
