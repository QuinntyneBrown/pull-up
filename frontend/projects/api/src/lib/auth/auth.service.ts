import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, defer, map, of, tap } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { AuthStorage } from './auth-storage';
import { IAuthService } from './auth.service.contract';
import { CompletePasswordResetRequest } from './complete-password-reset-request';
import { CurrentUser } from './current-user';
import { RefreshAccessTokenResponse } from './refresh-access-token-response';
import { RegisterUserRequest } from './register-user-request';
import { RegisterUserResponse } from './register-user-response';
import { RequestPasswordResetRequest } from './request-password-reset-request';
import { SignInRequest } from './sign-in-request';
import { SignInResponse } from './sign-in-response';

@Injectable({ providedIn: 'root' })
export class AuthService implements IAuthService {
  private readonly accessToken: BehaviorSubject<string | null>;
  private readonly currentUser: BehaviorSubject<CurrentUser | null>;

  readonly accessToken$: Observable<string | null>;
  readonly currentUser$: Observable<CurrentUser | null>;
  readonly isAuthenticated$: Observable<boolean>;

  constructor(
    private readonly http: HttpClient,
    private readonly storage: AuthStorage,
    @Inject(API_BASE_URL) private readonly baseUrl: string,
  ) {
    this.accessToken = new BehaviorSubject<string | null>(this.storage.getAccessToken());
    this.currentUser = new BehaviorSubject<CurrentUser | null>(this.storage.getUser());
    this.accessToken$ = this.accessToken.asObservable();
    this.currentUser$ = this.currentUser.asObservable();
    this.isAuthenticated$ = this.accessToken$.pipe(map(t => t !== null));
  }

  register(request: RegisterUserRequest): Observable<RegisterUserResponse> {
    return this.http
      .post<RegisterUserResponse>(`${this.baseUrl}/api/users`, request)
      .pipe(tap(response => this.applyAccessToken(response.accessToken)));
  }

  signIn(request: SignInRequest): Observable<SignInResponse> {
    return this.http
      .post<SignInResponse>(`${this.baseUrl}/api/auth/sign-in`, request)
      .pipe(tap(response => {
        this.applyAccessToken(response.accessToken);
        this.storage.setRefreshToken(response.refreshToken);
      }));
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http
      .get<CurrentUser>(`${this.baseUrl}/api/users/me`)
      .pipe(tap(user => this.applyUser(user)));
  }

  requestPasswordReset(request: RequestPasswordResetRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/auth/password-reset`, request);
  }

  completePasswordReset(request: CompletePasswordResetRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/auth/password-reset/confirm`, request);
  }

  refresh(refreshToken: string): Observable<RefreshAccessTokenResponse> {
    return this.http
      .post<RefreshAccessTokenResponse>(`${this.baseUrl}/api/auth/refresh`, { refreshToken })
      .pipe(tap(response => {
        this.applyAccessToken(response.accessToken);
        this.storage.setRefreshToken(response.refreshToken);
      }));
  }

  signOut(): Observable<void> {
    return defer(() => {
      const refreshToken = this.storage.getRefreshToken();
      if (!refreshToken) {
        this.clearSession();
        return of(void 0);
      }
      return this.http
        .post<void>(`${this.baseUrl}/api/auth/sign-out`, { refreshToken })
        .pipe(tap({ next: () => this.clearSession(), error: () => this.clearSession() }));
    });
  }

  clearSession(): void {
    this.storage.clear();
    this.accessToken.next(null);
    this.currentUser.next(null);
  }

  snapshotAccessToken(): string | null {
    return this.accessToken.value;
  }

  snapshotRefreshToken(): string | null {
    return this.storage.getRefreshToken();
  }

  private applyAccessToken(token: string): void {
    this.storage.setAccessToken(token);
    this.accessToken.next(token);
  }

  private applyUser(user: CurrentUser): void {
    this.storage.setUser(user);
    this.currentUser.next(user);
  }
}
