import { GuestSummary } from './guest-summary';
import { HostSummary } from './host-summary';
import { RsvpStatus } from './rsvp-status';

export interface EventDetail {
  readonly id: string;
  readonly title: string;
  readonly startsAtUtc: string;
  readonly endsAtUtc: string | null;
  readonly location: string;
  readonly description: string;
  readonly status: string;
  readonly allowPlusOne: boolean;
  readonly showGuestList: boolean;
  readonly host: HostSummary;
  readonly isHost: boolean;
  readonly myRsvpStatus: RsvpStatus | null;
  readonly goingCount: number;
  readonly maybeCount: number;
  readonly cantGoCount: number;
  readonly guests: ReadonlyArray<GuestSummary> | null;
}
