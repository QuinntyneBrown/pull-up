// Acceptance test for FT-003 — covers L2-018..L2-036, L2-039 indirectly:
// each EventsService method posts to the expected URL with the expected body
// and parses the response shape.

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { API_BASE_URL } from '../api-base-url.token';
import { EventsService } from './events.service';

const BASE = 'http://api.test';
const EVENT_ID = '11111111-1111-1111-1111-111111111111';
const INV_ID = '22222222-2222-2222-2222-222222222222';

describe('EventsService', () => {
  let service: EventsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    service = TestBed.inject(EventsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list GETs /api/events and parses the four-bucket response', async () => {
    const promise = firstValueFrom(service.list());
    const req = http.expectOne(r => r.url === `${BASE}/api/events`);
    expect(req.request.method).toBe('GET');
    req.flush({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] });
    const r = await promise;
    expect(r).toEqual({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] });
  });

  it('list passes scope as a query param when provided', async () => {
    const promise = firstValueFrom(service.list('Hosting'));
    const req = http.expectOne(r => r.url === `${BASE}/api/events` && r.params.get('scope') === 'Hosting');
    req.flush({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] });
    await promise;
  });

  it('get GETs /api/events/{id} and parses the detail shape', async () => {
    const promise = firstValueFrom(service.get(EVENT_ID));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}`);
    expect(req.request.method).toBe('GET');
    req.flush({
      id: EVENT_ID, title: 'Picnic', startsAtUtc: '2026-06-01T17:00:00Z', endsAtUtc: null,
      location: 'Park', description: '', status: 'Scheduled', allowPlusOne: false, showGuestList: true,
      host: { userId: 'u1', fullName: 'A B', displayName: 'A', email: 'a@b.co' },
      isHost: true, myRsvpStatus: 'Going', goingCount: 1, maybeCount: 0, cantGoCount: 0, guests: [],
    });
    const detail = await promise;
    expect(detail.title).toBe('Picnic');
    expect(detail.isHost).toBe(true);
  });

  it('create POSTs the request body and returns { id }', async () => {
    const body = {
      title: 'Picnic', startsAtUtc: '2026-06-01T17:00:00Z', endsAtUtc: null,
      location: 'Park', description: null, allowPlusOne: false, showGuestList: true, inviteeEmails: null,
    };
    const promise = firstValueFrom(service.create(body));
    const req = http.expectOne(`${BASE}/api/events`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: EVENT_ID }, { status: 201, statusText: 'Created' });
    const r = await promise;
    expect(r.id).toBe(EVENT_ID);
  });

  it('update PUTs the request body to /api/events/{id}', async () => {
    const body = {
      title: 'Picnic 2', startsAtUtc: '2026-06-02T17:00:00Z', endsAtUtc: null,
      location: 'Park', description: null, allowPlusOne: false, showGuestList: true,
    };
    const promise = firstValueFrom(service.update(EVENT_ID, body));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('cancel POSTs to /api/events/{id}/cancel', async () => {
    const promise = firstValueFrom(service.cancel(EVENT_ID));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('addInvitee POSTs email to /api/events/{id}/invitees', async () => {
    const promise = firstValueFrom(service.addInvitee(EVENT_ID, { email: 'x@b.co' }));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}/invitees`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'x@b.co' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('removeInvitee DELETEs /api/events/{id}/invitees/{invId}', async () => {
    const promise = firstValueFrom(service.removeInvitee(EVENT_ID, INV_ID));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}/invitees/${INV_ID}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });

  it('setRsvp PUTs status + note to /api/events/{id}/rsvp', async () => {
    const promise = firstValueFrom(service.setRsvp(EVENT_ID, { status: 'Maybe', note: 'might be late' }));
    const req = http.expectOne(`${BASE}/api/events/${EVENT_ID}/rsvp`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ status: 'Maybe', note: 'might be late' });
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });
});
