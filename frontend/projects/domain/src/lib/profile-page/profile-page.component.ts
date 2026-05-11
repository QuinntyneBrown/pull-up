import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AUTH_SERVICE,
  CurrentUser,
  IAuthService,
  IProfileService,
  PROFILE_SERVICE,
} from 'api';
import {
  AppBarComponent,
  BottomNavBarComponent,
  NavItem,
  NavRailComponent,
} from 'components';
import { switchMap } from 'rxjs';
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
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
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

  constructor(
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
    @Inject(PROFILE_SERVICE) private readonly profile: IProfileService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.loadUser();
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
}
