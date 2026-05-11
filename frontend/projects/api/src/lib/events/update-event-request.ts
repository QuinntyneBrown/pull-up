export interface UpdateEventRequest {
  readonly title: string;
  readonly startsAtUtc: string;
  readonly endsAtUtc: string | null;
  readonly location: string;
  readonly description: string | null;
  readonly allowPlusOne: boolean;
  readonly showGuestList: boolean;
}
