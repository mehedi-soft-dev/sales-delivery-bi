import { Routes } from '@angular/router';
import { Shell } from './layout/shell';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'pipeline' },
      {
        path: 'pipeline',
        loadComponent: () => import('./features/quotation-pipeline/pipeline.page').then((m) => m.PipelinePage),
      },
      {
        path: 'conversion',
        loadComponent: () => import('./features/quotation-conversion/conversion.page').then((m) => m.ConversionPage),
      },
      {
        path: 'aging',
        loadComponent: () => import('./features/quotation-aging/aging.page').then((m) => m.AgingPage),
      },
    ],
  },
  {
    path: '403',
    loadComponent: () => import('./core/not-authorized/not-authorized.page').then((m) => m.NotAuthorizedPage),
  },
  {
    path: '**',
    loadComponent: () => import('./core/not-found/not-found.page').then((m) => m.NotFoundPage),
  },
];
