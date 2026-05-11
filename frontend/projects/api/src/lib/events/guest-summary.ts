import { RsvpStatus } from './rsvp-status';

export interface GuestSummary {
  readonly userId: string | null;
  readonly email: string;
  readonly fullName: string | null;
  readonly displayName: string | null;
  readonly rsvpStatus: RsvpStatus | null;
  readonly note: string | null;
}
