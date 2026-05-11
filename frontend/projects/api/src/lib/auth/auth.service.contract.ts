import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CompletePasswordResetRequest } from './complete-password-reset-request';
import { CurrentUser } from './current-user';
import { RefreshAccessTokenResponse } from './refresh-access-token-response';
import { RegisterUserRequest } from './register-user-request';
import { RegisterUserResponse } from './register-user-response';
import { RequestPasswordResetRequest } from './request-password-reset-request';
import { SignInRequest } from './sign-in-request';
import { SignInResponse } from './sign-in-response';

export interface IAuthService {
  readonly accessToken$: Observable<string | null>;
  readonly currentUser$: Observable<CurrentUser | null>;
  readonly isAuthenticated$: Observable<boolean>;

  register(request: RegisterUserRequest): Observable<RegisterUserResponse>;
  signIn(request: SignInRequest): Observable<SignInResponse>;
  loadCurrentUser(): Observable<CurrentUser>;
  requestPasswordReset(request: RequestPasswordResetRequest): Observable<void>;
  completePasswordReset(request: CompletePasswordResetRequest): Observable<void>;
  refresh(refreshToken: string): Observable<RefreshAccessTokenResponse>;
  signOut(): Observable<void>;
  clearSession(): void;
  snapshotAccessToken(): string | null;
  snapshotRefreshToken(): string | null;
}

export const AUTH_SERVICE = new InjectionToken<IAuthService>('AUTH_SERVICE');
