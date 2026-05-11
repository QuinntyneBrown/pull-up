import { CommonModule, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EVENTS_SERVICE, EventDetail, GuestSummary, IEventsService, RsvpStatus } from 'api';
import {
  AppBarComponent,
  AvatarEntry,
  AvatarTone,
  BottomNavBarComponent,
  NavItem,
  NavRailComponent,
  RsvpAvatarStackComponent,
  SegmentedButtonComponent,
  SegmentedOption,
} from 'components';

const NAV_ITEMS: ReadonlyArray<NavItem> = [
  { route: '/home', icon: 'event', label: 'Events' },
  { route: '/profile', icon: 'person', label: 'Profile' },
];

const RSVP_OPTIONS: ReadonlyArray<SegmentedOption<RsvpStatus>> = [
  { value: 'Going', label: 'Going', icon: 'check_circle' },
  { value: 'Maybe', label: 'Maybe', icon: 'help' },
  { value: 'CantGo', label: "Can't go", icon: 'cancel' },
];

@Component({
  selector: 'pu-confirm-cancel-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Cancel this event?</h2>
    <mat-dialog-content>
      <p>This action cannot be undone. Guests will be notified.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button mat-dialog-close data-testid="cancel-dialog-keep">Keep event</button>
      <button mat-flat-button color="warn" [mat-dialog-close]="true" data-testid="cancel-dialog-confirm">
        Yes, cancel it
      </button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmCancelDialogComponent {}

@Component({
  selector: 'pu-event-detail-page',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    AppBarComponent,
    BottomNavBarComponent,
    NavRailComponent,
    RsvpAvatarStackComponent,
    SegmentedButtonComponent,
  ],
  templateUrl: './event-detail-page.component.html',
  styleUrls: ['./event-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventDetailPageComponent implements OnInit {
  readonly navItems = NAV_ITEMS;
  readonly rsvpOptions = RSVP_OPTIONS;
  readonly loading = signal(true);
  readonly errored = signal(false);
  readonly event = signal<EventDetail | null>(null);
  readonly rsvpSubmitting = signal(false);
  readonly cancelSubmitting = signal(false);

  private eventId = '';

  constructor(
    private readonly route: ActivatedRoute,
    @Inject(EVENTS_SERVICE) private readonly eventsService: IEventsService,
    private readonly snackBar: MatSnackBar,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadEvent();
  }

  get isPast(): boolean {
    const event = this.event();
    if (!event) {
      return false;
    }

    return event.status === 'Cancelled' || new Date(event.startsAtUtc) < new Date();
  }

  get activeRoute(): string {
    return `/events/${this.eventId}`;
  }

  get avatarEntries(): ReadonlyArray<AvatarEntry> {
    const event = this.event();
    if (!event?.guests) {
      return [];
    }

    const tones: AvatarTone[] = ['primary', 'secondary', 'tertiary'];
    return event.guests.map((guest, index) => ({
      initials: this.initials(guest),
      tone: tones[index % tones.length],
    }));
  }

  onRsvpChange(value: string): void {
    const status = value as RsvpStatus;
    this.rsvpSubmitting.set(true);
    this.eventsService.setRsvp(this.eventId, { status, note: null }).subscribe({
      next: () => {
        this.rsvpSubmitting.set(false);
        this.loadEvent();
      },
      error: () => {
        this.rsvpSubmitting.set(false);
        this.snackBar.open('Could not save RSVP. Please try again.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  onCancelEvent(): void {
    const ref = this.dialog.open(ConfirmCancelDialogComponent, { width: '360px' });
    ref.afterClosed().subscribe((confirmed: boolean | undefined) => {
      if (!confirmed) return;
      this.cancelSubmitting.set(true);
      this.eventsService.cancel(this.eventId).subscribe({
        next: () => {
          this.cancelSubmitting.set(false);
          this.loadEvent();
        },
        error: () => {
          this.cancelSubmitting.set(false);
          this.snackBar.open('Could not cancel event. Please try again.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  retry(): void {
    this.loadEvent();
  }

  private loadEvent(): void {
    this.loading.set(true);
    this.errored.set(false);
    this.eventsService.get(this.eventId).subscribe({
      next: event => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errored.set(true);
      },
    });
  }

  private initials(guest: GuestSummary): string {
    const name = guest.displayName ?? guest.fullName ?? guest.email;
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }

    return name.substring(0, 2).toUpperCase();
  }
}
