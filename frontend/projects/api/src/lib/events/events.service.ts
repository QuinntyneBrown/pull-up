import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { AddInviteeRequest } from './add-invitee-request';
import { CreateEventRequest } from './create-event-request';
import { CreateEventResponse } from './create-event-response';
import { EventDetail } from './event-detail';
import { IEventsService } from './events.service.contract';
import { ListMyEventsResponse } from './list-my-events-response';
import { SetRsvpRequest } from './set-rsvp-request';
import { UpdateEventRequest } from './update-event-request';

@Injectable({ providedIn: 'root' })
export class EventsService implements IEventsService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string,
  ) {}

  list(scope?: string | null): Observable<ListMyEventsResponse> {
    const params = scope ? new HttpParams().set('scope', scope) : undefined;
    return this.http.get<ListMyEventsResponse>(`${this.baseUrl}/api/events`, { params });
  }

  get(id: string): Observable<EventDetail> {
    return this.http.get<EventDetail>(`${this.baseUrl}/api/events/${id}`);
  }

  create(request: CreateEventRequest): Observable<CreateEventResponse> {
    return this.http.post<CreateEventResponse>(`${this.baseUrl}/api/events`, request);
  }

  update(id: string, request: UpdateEventRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/events/${id}`, request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/events/${id}/cancel`, {});
  }

  addInvitee(id: string, request: AddInviteeRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/events/${id}/invitees`, request);
  }

  removeInvitee(id: string, invitationId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/events/${id}/invitees/${invitationId}`);
  }

  setRsvp(id: string, request: SetRsvpRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/events/${id}/rsvp`, request);
  }
}
