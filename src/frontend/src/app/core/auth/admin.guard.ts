import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from './current-user.service';
import { PermissionCodes } from './permission-codes';

export const adminGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  return currentUser.hasPermission(PermissionCodes.AdminView) || router.createUrlTree(['/403']);
};
