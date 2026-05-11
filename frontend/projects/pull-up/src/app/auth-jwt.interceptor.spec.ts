// Acceptance test for FT-004 — covers L2-006 (token refresh on 401).
// Verifies: (a) Authorization header is attached when a token is present;
// (b) a 401 on a protected endpoint triggers exactly one refresh attempt;
// (c) on refresh success the original request is retried with the new bearer;
// (d) on refresh failure tokens are cleared and /sign-in is navigated to.

import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { API_BASE_URL, AUTH_SERVICE, AuthService, AuthStorage } from 'api';
import { authJwtInterceptor } from './auth-jwt.interceptor';

const BASE = 'http://api.test';

describe('authJwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let storage: AuthStorage;
  let auth: AuthService;
  let navigateSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    localStorage.clear();
    navigateSpy = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authJwtInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
        { provide: AUTH_SERVICE, useExisting: AuthService },
        { provide: Router, useValue: { navigateByUrl: navigateSpy } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(AuthStorage);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('attaches the access token as a Bearer header on outgoing requests', async () => {
    storage.setAccessToken('access-1');
    // Rehydrate AuthService's internal subject from storage.
    auth['accessToken'].next('access-1');

    const promise = firstValueFrom(http.get(`${BASE}/api/users/me`));
    const req = httpMock.expectOne(`${BASE}/api/users/me`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-1');
    req.flush({});
    await promise;
  });

  it('on 401 calls /api/auth/refresh exactly once, retries with the new bearer, succeeds', async () => {
    storage.setAccessToken('old-access');
    storage.setRefreshToken('refresh-1');
    auth['accessToken'].next('old-access');

    const promise = firstValueFrom(http.get(`${BASE}/api/users/me`));

    const first = httpMock.expectOne(`${BASE}/api/users/me`);
    expect(first.request.headers.get('Authorization')).toBe('Bearer old-access');
    first.flush({ message: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(`${BASE}/api/auth/refresh`);
    expect(refresh.request.method).toBe('POST');
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush({
      accessToken: 'new-access', accessTokenExpiresAt: '2026-01-01T00:00:00Z',
      refreshToken: 'refresh-2', refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
    });

    const retried = httpMock.expectOne(`${BASE}/api/users/me`);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer new-access');
    retried.flush({ userId: 'u1', email: 'a@b.co' });

    await promise;
    expect(storage.getRefreshToken()).toBe('refresh-2');
  });

  it('on refresh failure clears session tokens and navigates to /sign-in', async () => {
    storage.setAccessToken('old-access');
    storage.setRefreshToken('refresh-1');
    storage.setUser({ userId: 'u1', email: 'a@b.co', fullName: 'A B', displayName: 'A', role: 'Member', createdAt: '2026-01-01T00:00:00Z' });
    auth['accessToken'].next('old-access');

    const promise = firstValueFrom(http.get(`${BASE}/api/users/me`)).catch(e => e);

    const first = httpMock.expectOne(`${BASE}/api/users/me`);
    first.flush({}, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(`${BASE}/api/auth/refresh`);
    refresh.flush({}, { status: 401, statusText: 'Unauthorized' });

    await promise;
    expect(storage.getAccessToken()).toBeNull();
    expect(storage.getRefreshToken()).toBeNull();
    expect(storage.getUser()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/sign-in');
  });

  it('does not attempt to refresh when the failing request is itself /api/auth/sign-in', async () => {
    const promise = firstValueFrom(http.post(`${BASE}/api/auth/sign-in`, { email: 'a@b.co', password: 'bad' })).catch(e => e);
    const req = httpMock.expectOne(`${BASE}/api/auth/sign-in`);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });
    await promise;
    httpMock.expectNone(`${BASE}/api/auth/refresh`);
  });
});
