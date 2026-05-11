import { RsvpStatus } from './rsvp-status';

export interface EventSummary {
  readonly id: string;
  readonly title: string;
  readonly startsAtUtc: string;
  readonly location: string;
  readonly status: string;
  readonly isHost: boolean;
  readonly myRsvpStatus: RsvpStatus | null;
}
