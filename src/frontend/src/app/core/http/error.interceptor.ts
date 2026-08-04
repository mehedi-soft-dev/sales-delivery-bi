import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { CurrentUserService } from '../auth/current-user.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const messageService = inject(MessageService);
  const currentUser = inject(CurrentUserService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 403) {
          void router.navigateByUrl('/403');
        } else if (error.status === 401 && !req.url.endsWith('/auth/login')) {
          // Expired/invalid token on an already-authenticated request — the login endpoint's own 401
          // (wrong credentials) is handled inline by the login page, not here.
          currentUser.clearToken();
          void router.navigateByUrl('/login');
        } else if (error.status === 404 || error.status >= 500) {
          messageService.add({
            severity: 'error',
            summary: error.status === 404 ? 'Not found' : 'Something went wrong',
            detail: error.error?.detail ?? 'Please try again.',
          });
        }
      }
      return throwError(() => error);
    }),
  );
};
