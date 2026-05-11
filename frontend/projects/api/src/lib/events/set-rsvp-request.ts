import { RsvpStatus } from './rsvp-status';

export interface SetRsvpRequest {
  readonly status: RsvpStatus;
  readonly note: string | null;
}
