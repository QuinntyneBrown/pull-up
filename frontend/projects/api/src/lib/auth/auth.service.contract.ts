import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CurrentUser } from './current-user';
import { RegisterUserRequest } from './register-user-request';
import { RegisterUserResponse } from './register-user-response';

export interface IAuthService {
  readonly accessToken$: Observable<string | null>;
  readonly currentUser$: Observable<CurrentUser | null>;
  readonly isAuthenticated$: Observable<boolean>;

  register(request: RegisterUserRequest): Observable<RegisterUserResponse>;
  loadCurrentUser(): Observable<CurrentUser>;
  signOut(): void;
  snapshotAccessToken(): string | null;
}

export const AUTH_SERVICE = new InjectionToken<IAuthService>('AUTH_SERVICE');
