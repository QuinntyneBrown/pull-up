import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { CurrentUser } from './current-user';
import { IAuthService } from './auth.service.contract';
import { RegisterUserRequest } from './register-user-request';
import { RegisterUserResponse } from './register-user-response';

const TOKEN_STORAGE_KEY = 'pullup.access-token';
const USER_STORAGE_KEY = 'pullup.current-user';

@Injectable({ providedIn: 'root' })
export class AuthService implements IAuthService {
  private readonly accessToken = new BehaviorSubject<string | null>(this.readToken());
  private readonly currentUser = new BehaviorSubject<CurrentUser | null>(this.readUser());

  readonly accessToken$ = this.accessToken.asObservable();
  readonly currentUser$ = this.currentUser.asObservable();
  readonly isAuthenticated$ = this.accessToken$.pipe(map(t => t !== null));

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string,
  ) {}

  register(request: RegisterUserRequest): Observable<RegisterUserResponse> {
    return this.http
      .post<RegisterUserResponse>(`${this.baseUrl}/api/users`, request)
      .pipe(tap(response => this.storeToken(response.accessToken)));
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http
      .get<CurrentUser>(`${this.baseUrl}/api/users/me`)
      .pipe(tap(user => this.storeUser(user)));
  }

  signOut(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_STORAGE_KEY);
    this.accessToken.next(null);
    this.currentUser.next(null);
  }

  snapshotAccessToken(): string | null {
    return this.accessToken.value;
  }

  private storeToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
    this.accessToken.next(token);
  }

  private storeUser(user: CurrentUser): void {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
    this.currentUser.next(user);
  }

  private readToken(): string | null {
    return typeof localStorage === 'undefined' ? null : localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  private readUser(): CurrentUser | null {
    if (typeof localStorage === 'undefined') return null;
    const raw = localStorage.getItem(USER_STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
