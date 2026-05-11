import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { BrandLogoComponent } from 'components';

interface SignInForm {
  email: string;
  password: string;
}

@Component({
  selector: 'pu-sign-in-page',
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
  templateUrl: './sign-in-page.component.html',
  styleUrls: ['./sign-in-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPageComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      password: ['', [Validators.required]],
    });
  }

  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.serverError.set(null);
    const value = this.form.getRawValue() as SignInForm;
    this.auth.signIn(value).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/home');
      },
      error: err => {
        this.submitting.set(false);
        const status = err?.status;
        if (status === 401) {
          this.serverError.set('Invalid email or password.');
        } else if (status === 429) {
          this.serverError.set('Too many attempts. Please wait a moment and try again.');
        } else {
          this.serverError.set('Sign-in failed. Please try again.');
        }
      },
    });
  }
}
