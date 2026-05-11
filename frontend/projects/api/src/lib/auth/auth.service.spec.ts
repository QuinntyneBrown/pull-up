// Acceptance test for FT-001 — covers L2-004, L2-005, L2-006, L2-007, L2-008, L2-009 indirectly:
// asserts AuthService posts each command to the correct URL with the correct body
// and parses the response shape (token + user fields) back into observable state.

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, beforeEach, afterEach, it, expect } from 'vitest';
import { API_BASE_URL } from '../api-base-url.token';
import { AuthService } from './auth.service';
import { AuthStorage } from './auth-storage';

const BASE = 'http://api.test';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let storage: AuthStorage;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(AuthStorage);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('signIn posts credentials and stores access + refresh tokens', async () => {
    const promise = firstValueFrom(service.signIn({ email: 'a@b.co', password: 'pw' }));
    const req = http.expectOne(`${BASE}/api/auth/sign-in`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.co', password: 'pw' });
    req.flush({
      userId: 'u1', email: 'a@b.co', fullName: 'A B', displayName: 'A',
      role: 'Member', accessToken: 'ax', accessTokenExpiresAt: '2026-01-01T00:00:00Z',
      refreshToken: 'rx', refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
    });
    const response = await promise;
    expect(response.accessToken).toBe('ax');
    expect(service.snapshotAccessToken()).toBe('ax');
    expect(storage.getRefreshToken()).toBe('rx');
  });

  it('requestPasswordReset posts to /password-reset', async () => {
    const promise = firstValueFrom(service.requestPasswordReset({ email: 'a@b.co' }));
    const req = http.expectOne(`${BASE}/api/auth/password-reset`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.co' });
    req.flush(null, { status: 202, statusText: 'Accepted' });
    await promise;
  });

  it('completePasswordReset posts token + new password', async () => {
    const promise = firstValueFrom(service.completePasswordReset({ token: 't', newPassword: 'NewPass123!' }));
    const req = http.expectOne(`${BASE}/api/auth/password-reset/confirm`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ token: 't', newPassword: 'NewPass123!' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('refresh posts refresh token and rotates access + refresh tokens', async () => {
    storage.setRefreshToken('old-refresh');
    const promise = firstValueFrom(service.refresh('old-refresh'));
    const req = http.expectOne(`${BASE}/api/auth/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'old-refresh' });
    req.flush({
      accessToken: 'new-ax', accessTokenExpiresAt: '2026-01-01T00:00:00Z',
      refreshToken: 'new-rx', refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
    });
    await promise;
    expect(service.snapshotAccessToken()).toBe('new-ax');
    expect(storage.getRefreshToken()).toBe('new-rx');
  });

  it('signOut posts the refresh token, then clears local state', async () => {
    storage.setAccessToken('ax');
    storage.setRefreshToken('rx');
    storage.setUser({ userId: 'u1', email: 'a@b.co', fullName: 'A B', displayName: 'A', role: 'Member', createdAt: '2026-01-01T00:00:00Z' });
    const promise = firstValueFrom(service.signOut());
    const req = http.expectOne(`${BASE}/api/auth/sign-out`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'rx' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
    expect(storage.getAccessToken()).toBeNull();
    expect(storage.getRefreshToken()).toBeNull();
    expect(storage.getUser()).toBeNull();
  });

  it('signOut without a stored refresh token clears locally and makes no HTTP call', async () => {
    storage.clear();
    await firstValueFrom(service.signOut());
    http.expectNone(`${BASE}/api/auth/sign-out`);
  });
});
