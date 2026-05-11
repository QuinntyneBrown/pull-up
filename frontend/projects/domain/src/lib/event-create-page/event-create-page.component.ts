import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { EVENTS_SERVICE, IEventsService } from 'api';
import { AppBarComponent } from 'components';

@Component({
  selector: 'pu-event-create-page',
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
  templateUrl: './event-create-page.component.html',
  styleUrls: ['./event-create-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventCreatePageComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly serverErrors = signal<Record<string, string[]> | null>(null);
  readonly inviteeEmails = signal<string[]>([]);
  readonly today = new Date();

  constructor(
    fb: FormBuilder,
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

  addInvitee(): void {
    const email = String(this.form.get('inviteeInput')?.value ?? '').trim();
    if (!email || !this.isValidEmail(email)) {
      return;
    }

    if (!this.inviteeEmails().includes(email)) {
      this.inviteeEmails.update(list => [...list, email]);
    }

    this.form.get('inviteeInput')?.setValue('');
  }

  removeInvitee(email: string): void {
    this.inviteeEmails.update(list => list.filter(value => value !== email));
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const date = raw.date;
    const time = String(raw.time ?? '');

    if (!date || !time) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverErrors.set(null);

    const [hours, minutes] = time.split(':').map(Number);
    const startsAt = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), hours, minutes));

    this.eventsService.create({
      title: String(raw.title ?? ''),
      startsAtUtc: startsAt.toISOString(),
      endsAtUtc: null,
      location: String(raw.location ?? ''),
      description: raw.description ? String(raw.description) : null,
      allowPlusOne: Boolean(raw.allowPlusOne),
      showGuestList: raw.showGuestList !== false,
      inviteeEmails: this.inviteeEmails().length > 0 ? this.inviteeEmails() : null,
    }).subscribe({
      next: response => {
        this.router.navigate(['/events', response.id]);
      },
      error: err => {
        this.submitting.set(false);
        if (err?.status === 400 && err?.error?.errors) {
          this.serverErrors.set(err.error.errors as Record<string, string[]>);
          return;
        }

        this.snackBar.open('Could not create event. Please try again.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  private timeValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value as string | null;
    if (!value) {
      return null;
    }

    return /^\d{2}:\d{2}$/.test(value) ? null : { invalidTime: true };
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}
