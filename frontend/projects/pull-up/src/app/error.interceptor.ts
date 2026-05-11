import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        const message = messageFor(err);
        if (message) {
          snackBar.open(message, 'Dismiss', { duration: 5000 });
        }
      }
      return throwError(() => err);
    }),
  );
};

function messageFor(err: HttpErrorResponse): string | null {
  if (err.status === 0) return 'Network error. Check your connection and try again.';
  if (err.status >= 500) return 'Something went wrong on our end. Please try again.';
  if (err.status === 403) return "You don't have permission to do that.";
  if (err.status === 404) return 'Not found.';
  if (err.status === 429) return 'Too many attempts. Try again in a moment.';
  // 400 = form/validation, surfaces inline.
  // 401 = handled by auth-jwt.interceptor (refresh + redirect).
  // 409 = handler-specific (duplicate email, event already passed), surfaced inline by callers.
  return null;
}
