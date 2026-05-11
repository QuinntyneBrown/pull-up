import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, Inject, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AUTH_SERVICE,
  CurrentUser,
  IAuthService,
  IProfileService,
  NotificationPreferences,
  PROFILE_SERVICE,
} from 'api';
import {
  AppBarComponent,
  BottomNavBarComponent,
  NavItem,
  NavRailComponent,
} from 'components';
import { debounceTime, switchMap } from 'rxjs';
import {
  EditProfileDialogComponent,
  EditProfileDialogResult,
} from './edit-profile-dialog.component';

const NAV_ITEMS: ReadonlyArray<NavItem> = [
  { route: '/home', icon: 'event', label: 'Events' },
  { route: '/profile', icon: 'person', label: 'Profile' },
];

@Component({
  selector: 'pu-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSlideToggleModule,
    AppBarComponent,
    BottomNavBarComponent,
    NavRailComponent,
  ],
  templateUrl: './profile-page.component.html',
  styleUrls: ['./profile-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePageComponent implements OnInit {
  readonly user = signal<CurrentUser | null>(null);
  readonly loading = signal(true);
  readonly errored = signal(false);
  readonly navItems = NAV_ITEMS;
  readonly activeRoute = '/profile';

  readonly prefsForm: FormGroup;
  readonly prefsLoaded = signal(false);

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
    @Inject(PROFILE_SERVICE) private readonly profile: IProfileService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
  ) {
    this.prefsForm = fb.nonNullable.group({
      newInvitations: [true],
      eventReminders: [true],
      rsvpChanges: [true],
    });
  }

  ngOnInit(): void {
    this.loadUser();
    this.loadPrefs();
    this.prefsForm.valueChanges
      .pipe(debounceTime(400), takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        if (!this.prefsLoaded()) return;
        this.profile.updateNotificationPreferences(value as NotificationPreferences).subscribe({
          error: () => this.snackBar.open('Could not save preferences. Please try again.', 'Dismiss', { duration: 4000 }),
        });
      });
  }

  openEdit(): void {
    const current = this.user();
    if (!current) return;
    const ref = this.dialog.open<
      EditProfileDialogComponent,
      { fullName: string; displayName: string },
      EditProfileDialogResult | undefined
    >(EditProfileDialogComponent, {
      data: { fullName: current.fullName, displayName: current.displayName },
      width: '420px',
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.profile.updateProfile(result).pipe(
        switchMap(() => this.auth.loadCurrentUser()),
      ).subscribe({
        next: u => {
          this.user.set(u);
          this.snackBar.open('Profile updated.', 'Dismiss', { duration: 4000 });
        },
        error: () => {
          this.snackBar.open('Could not save changes. Please try again.', 'Dismiss', { duration: 5000 });
        },
      });
    });
  }

  private loadUser(): void {
    this.loading.set(true);
    this.errored.set(false);
    this.auth.loadCurrentUser().subscribe({
      next: u => {
        this.user.set(u);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errored.set(true);
      },
    });
  }

  private loadPrefs(): void {
    this.profile.getNotificationPreferences().subscribe({
      next: prefs => {
        this.prefsForm.setValue(prefs, { emitEvent: false });
        this.prefsLoaded.set(true);
      },
      // On failure leave the defaults (all-on); user can still toggle.
      error: () => this.prefsLoaded.set(true),
    });
  }
}
