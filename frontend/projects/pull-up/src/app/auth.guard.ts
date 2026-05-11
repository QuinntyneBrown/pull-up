import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';

export const authGuard: CanActivateFn = () => {
  const auth = inject<IAuthService>(AUTH_SERVICE);
  const router = inject(Router);
  if (auth.snapshotAccessToken()) {
    return true;
  }
  return router.createUrlTree(['/sign-up']);
};
