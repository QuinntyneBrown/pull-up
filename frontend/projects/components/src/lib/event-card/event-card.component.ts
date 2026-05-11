import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export type EventCardRsvpStatus = 'Going' | 'Maybe' | 'CantGo';

@Component({
  selector: 'pu-event-card',
  standalone: true,
  imports: [DatePipe, MatIconModule],
  templateUrl: './event-card.component.html',
  styleUrls: ['./event-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventCardComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) startsAtUtc!: Date;
  @Input({ required: true }) location!: string;
  @Input() isHost: boolean = false;
  @Input() myRsvpStatus: EventCardRsvpStatus | null = null;

  get statusLabel(): string | null {
    if (!this.myRsvpStatus) return null;
    if (this.myRsvpStatus === 'CantGo') return "Can't go";
    return this.myRsvpStatus;
  }

  get statusModifier(): string {
    return this.myRsvpStatus
      ? `event-card__rsvp-badge--${this.myRsvpStatus.toLowerCase()}`
      : '';
  }
}
