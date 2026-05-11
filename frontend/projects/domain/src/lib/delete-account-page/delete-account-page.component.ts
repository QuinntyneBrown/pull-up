import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthStorage, IProfileService, PROFILE_SERVICE } from 'api';

@Component({
  selector: 'pu-delete-account-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    RouterLink,
  ],
  templateUrl: './delete-account-page.component.html',
  styleUrls: ['./delete-account-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteAccountPageComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);

  constructor(
    fb: FormBuilder,
    @Inject(PROFILE_SERVICE) private readonly profile: IProfileService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly authStorage: AuthStorage,
  ) {
    this.form = fb.nonNullable.group({
      currentPassword: ['', [Validators.required]],
      confirm: [false, [Validators.requiredTrue]],
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverError.set(null);

    const { currentPassword } = this.form.getRawValue();
    this.profile.deleteAccount({ currentPassword }).subscribe({
      next: () => {
        this.authStorage.clear();
        this.snackBar.open('Account deleted.', 'Dismiss', { duration: 5000 });
        this.router.navigateByUrl('/sign-up');
      },
      error: err => {
        this.submitting.set(false);
        this.serverError.set(
          err?.status === 401
            ? 'Current password is incorrect.'
            : 'Could not delete account. Please try again.',
        );
      },
    });
  }
}
