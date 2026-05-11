import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AUTH_SERVICE, IAuthService } from 'api';

export const authJwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject<IAuthService>(AUTH_SERVICE);
  const token = auth.snapshotAccessToken();
  if (!token) {
    return next(req);
  }
  const authed = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
  return next(authed);
};
