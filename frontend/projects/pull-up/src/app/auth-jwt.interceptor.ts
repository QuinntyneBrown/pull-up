import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AUTH_SERVICE, IAuthService } from 'api';

const RETRIED = new HttpContextToken<boolean>(() => false);

export const authJwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject<IAuthService>(AUTH_SERVICE);
  const router = inject(Router);

  const token = auth.snapshotAccessToken();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authedReq).pipe(
    catchError(err => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }
      if (req.context.get(RETRIED)) {
        return throwError(() => err);
      }
      if (req.url.includes('/api/auth/refresh') || req.url.includes('/api/auth/sign-in')) {
        return throwError(() => err);
      }
      const refresh = auth.snapshotRefreshToken();
      if (!refresh) {
        return throwError(() => err);
      }

      return auth.refresh(refresh).pipe(
        switchMap(() => {
          const newToken = auth.snapshotAccessToken();
          const retried = req.clone({
            setHeaders: newToken ? { Authorization: `Bearer ${newToken}` } : {},
            context: req.context.set(RETRIED, true),
          });
          return next(retried);
        }),
        catchError(refreshErr => {
          auth.clearSession();
          router.navigateByUrl('/sign-in');
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};
