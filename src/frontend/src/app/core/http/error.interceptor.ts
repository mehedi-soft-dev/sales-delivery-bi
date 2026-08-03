import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const messageService = inject(MessageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 403) {
          void router.navigateByUrl('/403');
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
