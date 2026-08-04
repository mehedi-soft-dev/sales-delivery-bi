import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from './current-user.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  if (currentUser.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
