import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { BrandLogoComponent } from 'components';

@Component({
  selector: 'pu-complete-password-reset-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    BrandLogoComponent,
  ],
  templateUrl: './complete-password-reset-page.component.html',
  styleUrls: ['./complete-password-reset-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompletePasswordResetPageComponent implements OnInit {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly missingToken = signal(false);

  private token: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
  ) {
    this.form = this.fb.nonNullable.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
    if (!this.token) {
      this.missingToken.set(true);
    }
  }

  get newPassword() { return this.form.get('newPassword')!; }

  submit(): void {
    if (this.form.invalid || this.submitting() || !this.token) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.serverError.set(null);
    const { newPassword } = this.form.getRawValue() as { newPassword: string };
    this.auth.completePasswordReset({ token: this.token, newPassword }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.snackBar.open('Password updated. Sign in with your new password.', 'Dismiss', { duration: 5000 });
        this.router.navigateByUrl('/sign-in');
      },
      error: err => {
        this.submitting.set(false);
        if (err?.status === 400) {
          this.serverError.set('This reset link is invalid or has expired. Request a new one.');
        } else {
          this.serverError.set('Could not reset your password. Please try again.');
        }
      },
    });
  }
}
