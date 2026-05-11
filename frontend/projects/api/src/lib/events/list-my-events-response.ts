import { EventSummary } from './event-summary';

export interface ListMyEventsResponse {
  readonly thisWeek: ReadonlyArray<EventSummary>;
  readonly laterThisMonth: ReadonlyArray<EventSummary>;
  readonly nextMonth: ReadonlyArray<EventSummary>;
  readonly past: ReadonlyArray<EventSummary>;
}
