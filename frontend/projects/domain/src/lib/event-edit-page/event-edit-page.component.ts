import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AddInviteeRequest,
  EVENTS_SERVICE,
  EventDetail,
  GuestSummary,
  IEventsService,
} from 'api';
import { AppBarComponent } from 'components';

interface PendingInvitee {
  email: string;
}

@Component({
  selector: 'pu-event-edit-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatSlideToggleModule,
    AppBarComponent,
  ],
  templateUrl: './event-edit-page.component.html',
  styleUrls: ['./event-edit-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventEditPageComponent implements OnInit {
  readonly form: FormGroup;
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly serverErrors = signal<Record<string, string[]> | null>(null);
  readonly existingGuests = signal<ReadonlyArray<GuestSummary>>([]);
  readonly pendingInvitees = signal<ReadonlyArray<PendingInvitee>>([]);
  readonly removingInvitees = signal<ReadonlyArray<string>>([]);
  readonly today = new Date();

  eventId = '';

  constructor(
    fb: FormBuilder,
    private readonly route: ActivatedRoute,
    @Inject(EVENTS_SERVICE) private readonly eventsService: IEventsService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
  ) {
    this.form = fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      date: [null as Date | null, [Validators.required]],
      time: ['', [Validators.required, this.timeValidator]],
      location: ['', [Validators.required, Validators.maxLength(300)]],
      description: [''],
      allowPlusOne: [false],
      showGuestList: [true],
      inviteeInput: [''],
    });
  }

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadEvent();
  }

  addInvitee(): void {
    const email = String(this.form.get('inviteeInput')?.value ?? '').trim();
    if (!email || !this.isValidEmail(email)) return;

    const alreadyInvited = this.existingGuests().some(g => g.email === email);
    const alreadyPending = this.pendingInvitees().some(i => i.email === email);
    if (alreadyInvited || alreadyPending) {
      this.form.get('inviteeInput')?.setValue('');
      return;
    }

    this.pendingInvitees.update(list => [...list, { email }]);
    this.form.get('inviteeInput')?.setValue('');
  }

  removePendingInvitee(email: string): void {
    this.pendingInvitees.update(list => list.filter(i => i.email !== email));
  }

  removeExistingInvitee(invitationId: string): void {
    this.removingInvitees.update(ids => [...ids, invitationId]);
    this.eventsService.removeInvitee(this.eventId, invitationId).subscribe({
      next: () => {
        this.existingGuests.update(guests =>
          guests.filter(g => g.invitationId !== invitationId),
        );
        this.removingInvitees.update(ids => ids.filter(id => id !== invitationId));
      },
      error: () => {
        this.removingInvitees.update(ids => ids.filter(id => id !== invitationId));
        this.snackBar.open('Could not remove invitee. Please try again.', 'Dismiss', {
          duration: 4000,
        });
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const date = raw.date as Date | null;
    const time = String(raw.time ?? '');

    if (!date || !time) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverErrors.set(null);

    const [hours, minutes] = time.split(':').map(Number);
    const startsAt = new Date(
      Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), hours, minutes),
    );

    this.eventsService
      .update(this.eventId, {
        title: String(raw.title ?? ''),
        startsAtUtc: startsAt.toISOString(),
        endsAtUtc: null,
        location: String(raw.location ?? ''),
        description: raw.description ? String(raw.description) : null,
        allowPlusOne: Boolean(raw.allowPlusOne),
        showGuestList: raw.showGuestList !== false,
      })
      .subscribe({
        next: () => this.addPendingInviteesAndNavigate(),
        error: err => {
          this.submitting.set(false);
          if (err?.status === 400 && err?.error?.errors) {
            this.serverErrors.set(err.error.errors as Record<string, string[]>);
            return;
          }
          this.snackBar.open('Could not save event. Please try again.', 'Dismiss', {
            duration: 5000,
          });
        },
      });
  }

  isRemoving(invitationId: string): boolean {
    return this.removingInvitees().includes(invitationId);
  }

  private addPendingInviteesAndNavigate(): void {
    const pending = this.pendingInvitees();
    if (pending.length === 0) {
      this.router.navigate(['/events', this.eventId]);
      return;
    }

    const requests = pending.map(i =>
      this.eventsService.addInvitee(this.eventId, { email: i.email } as AddInviteeRequest),
    );

    let remaining = requests.length;
    const done = () => {
      remaining -= 1;
      if (remaining === 0) {
        this.router.navigate(['/events', this.eventId]);
      }
    };

    for (const req of requests) {
      req.subscribe({ next: done, error: done });
    }
  }

  private loadEvent(): void {
    this.loading.set(true);
    this.eventsService.get(this.eventId).subscribe({
      next: (event: EventDetail) => {
        const startsAt = new Date(event.startsAtUtc);
        const hours = String(startsAt.getUTCHours()).padStart(2, '0');
        const minutes = String(startsAt.getUTCMinutes()).padStart(2, '0');

        this.form.patchValue({
          title: event.title,
          date: new Date(startsAt.getFullYear(), startsAt.getMonth(), startsAt.getDate()),
          time: `${hours}:${minutes}`,
          location: event.location,
          description: event.description ?? '',
          allowPlusOne: event.allowPlusOne,
          showGuestList: event.showGuestList,
        });

        this.existingGuests.set(event.guests ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load event.', 'Dismiss', { duration: 5000 });
        this.router.navigate(['/events', this.eventId]);
      },
    });
  }

  private timeValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value as string | null;
    if (!value) return null;
    return /^\d{2}:\d{2}$/.test(value) ? null : { invalidTime: true };
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}
