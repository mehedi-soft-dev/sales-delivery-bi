import { Routes } from '@angular/router';
import { Shell } from './layout/shell';
import { authGuard } from './core/auth/auth.guard';
import { adminGuard } from './core/auth/admin.guard';

export const routes: Routes = [
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./features/executive-overview/executive-overview.page').then((m) => m.ExecutiveOverviewPage),
      },
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
      {
        path: 'reports',
        loadComponent: () => import('./features/coming-soon/coming-soon.page').then((m) => m.ComingSoonPage),
        data: { title: 'Report', icon: 'chart-pie' },
      },
      {
        path: 'dashboard/sales-orders',
        loadComponent: () => import('./features/coming-soon/coming-soon.page').then((m) => m.ComingSoonPage),
        data: { title: 'Sales Orders', icon: 'shopping-cart' },
      },
      {
        path: 'dashboard/delivery',
        loadComponent: () => import('./features/coming-soon/coming-soon.page').then((m) => m.ComingSoonPage),
        data: { title: 'Delivery / Challan', icon: 'truck' },
      },
      {
        path: 'dashboard/invoice',
        loadComponent: () => import('./features/coming-soon/coming-soon.page').then((m) => m.ComingSoonPage),
        data: { title: 'Sales Invoice', icon: 'receipt' },
      },
      {
        path: 'dashboard/return',
        loadComponent: () => import('./features/coming-soon/coming-soon.page').then((m) => m.ComingSoonPage),
        data: { title: 'Return / Credit Note', icon: 'reply' },
      },
      {
        path: 'admin/users',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/users/users.page').then((m) => m.UsersPage),
      },
      {
        path: 'admin/roles',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/roles/roles.page').then((m) => m.RolesPage),
      },
      {
        path: 'admin/permissions',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/permissions/permissions.page').then((m) => m.PermissionsPage),
      },
    ],
  },
  {
    path: 'login',
    loadComponent: () => import('./core/auth/login.page').then((m) => m.LoginPage),
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
