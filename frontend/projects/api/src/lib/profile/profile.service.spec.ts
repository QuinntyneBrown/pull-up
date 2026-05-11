// Acceptance test for FT-002 — covers L2-012 (profile edit), L2-013 (email change),
// L2-014/L2-015 (delete account), L2-016/L2-017 (notification preferences).
// Asserts ProfileService posts each command to the correct URL with the correct body
// and parses the response shape.

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { API_BASE_URL } from '../api-base-url.token';
import { ProfileService } from './profile.service';

const BASE = 'http://api.test';

describe('ProfileService', () => {
  let service: ProfileService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    service = TestBed.inject(ProfileService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('updateProfile PUTs name + displayName to /api/users/me/profile', async () => {
    const promise = firstValueFrom(service.updateProfile({ fullName: 'Ada Lovelace', displayName: 'Ada' }));
    const req = http.expectOne(`${BASE}/api/users/me/profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ fullName: 'Ada Lovelace', displayName: 'Ada' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('requestEmailChange POSTs new email + current password to /api/users/me/email-change', async () => {
    const promise = firstValueFrom(service.requestEmailChange({ newEmail: 'new@b.co', currentPassword: 'pw' }));
    const req = http.expectOne(`${BASE}/api/users/me/email-change`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ newEmail: 'new@b.co', currentPassword: 'pw' });
    req.flush(null, { status: 202, statusText: 'Accepted' });
    await promise;
  });

  it('confirmEmailChange POSTs token to /api/users/me/email-change/confirm', async () => {
    const promise = firstValueFrom(service.confirmEmailChange({ token: 't' }));
    const req = http.expectOne(`${BASE}/api/users/me/email-change/confirm`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ token: 't' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('deleteAccount DELETEs /api/users/me with current password in body', async () => {
    const promise = firstValueFrom(service.deleteAccount({ currentPassword: 'pw' }));
    const req = http.expectOne(`${BASE}/api/users/me`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ currentPassword: 'pw' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('getNotificationPreferences GETs and parses the three-bool response', async () => {
    const promise = firstValueFrom(service.getNotificationPreferences());
    const req = http.expectOne(`${BASE}/api/users/me/notification-preferences`);
    expect(req.request.method).toBe('GET');
    req.flush({ newInvitations: true, eventReminders: false, rsvpChanges: true });
    const prefs = await promise;
    expect(prefs).toEqual({ newInvitations: true, eventReminders: false, rsvpChanges: true });
  });

  it('updateNotificationPreferences PUTs the three-bool body', async () => {
    const promise = firstValueFrom(service.updateNotificationPreferences({
      newInvitations: false, eventReminders: true, rsvpChanges: false,
    }));
    const req = http.expectOne(`${BASE}/api/users/me/notification-preferences`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ newInvitations: false, eventReminders: true, rsvpChanges: false });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });
});
