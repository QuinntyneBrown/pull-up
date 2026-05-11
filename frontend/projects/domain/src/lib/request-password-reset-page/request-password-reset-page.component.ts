import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { BrandLogoComponent } from 'components';

@Component({
  selector: 'pu-request-password-reset-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    BrandLogoComponent,
    RouterLink,
  ],
  templateUrl: './request-password-reset-page.component.html',
  styleUrls: ['./request-password-reset-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestPasswordResetPageComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly submitted = signal(false);

  constructor(
    private readonly fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    });
  }

  get email() { return this.form.get('email')!; }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { email } = this.form.getRawValue() as { email: string };
    this.auth.requestPasswordReset({ email }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: () => {
        // No enumeration: same universal success state on any error.
        this.submitting.set(false);
        this.submitted.set(true);
      },
    });
  }
}
