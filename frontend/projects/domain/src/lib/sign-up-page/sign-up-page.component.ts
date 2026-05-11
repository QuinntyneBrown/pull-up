import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { BrandLogoComponent } from 'components';

interface SignUpForm {
  fullName: string;
  email: string;
  password: string;
}

@Component({
  selector: 'pu-sign-up-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    BrandLogoComponent,
  ],
  templateUrl: './sign-up-page.component.html',
  styleUrls: ['./sign-up-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPageComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
  ) {
    this.form = this.fb.nonNullable.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  get fullName() { return this.form.get('fullName')!; }
  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.serverError.set(null);
    const value = this.form.getRawValue() as SignUpForm;
    this.auth.register(value).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/home');
      },
      error: err => {
        this.submitting.set(false);
        const status = err?.status;
        if (status === 409) {
          this.serverError.set('An account with this email already exists.');
        } else if (status === 400) {
          this.serverError.set('Please check the highlighted fields.');
        } else {
          this.serverError.set('Sign-up failed. Please try again.');
        }
      },
    });
  }
}
