import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { AddInviteeRequest } from './add-invitee-request';
import { CreateEventRequest } from './create-event-request';
import { CreateEventResponse } from './create-event-response';
import { EventDetail } from './event-detail';
import { ListMyEventsResponse } from './list-my-events-response';
import { SetRsvpRequest } from './set-rsvp-request';
import { UpdateEventRequest } from './update-event-request';

export interface IEventsService {
  list(scope?: string | null): Observable<ListMyEventsResponse>;
  get(id: string): Observable<EventDetail>;
  create(request: CreateEventRequest): Observable<CreateEventResponse>;
  update(id: string, request: UpdateEventRequest): Observable<void>;
  cancel(id: string): Observable<void>;
  addInvitee(id: string, request: AddInviteeRequest): Observable<void>;
  removeInvitee(id: string, invitationId: string): Observable<void>;
  setRsvp(id: string, request: SetRsvpRequest): Observable<void>;
}

export const EVENTS_SERVICE = new InjectionToken<IEventsService>('EVENTS_SERVICE');
