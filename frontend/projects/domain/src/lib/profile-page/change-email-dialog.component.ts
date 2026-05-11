import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ChangeEmailDialogResult {
  readonly newEmail: string;
  readonly currentPassword: string;
}

@Component({
  selector: 'pu-change-email-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './change-email-dialog.component.html',
  styleUrls: ['./change-email-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeEmailDialogComponent {
  readonly form: FormGroup;

  constructor(
    fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<ChangeEmailDialogComponent, ChangeEmailDialogResult | undefined>,
  ) {
    this.form = fb.nonNullable.group({
      newEmail: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      currentPassword: ['', [Validators.required]],
    });
  }

  get newEmail() { return this.form.get('newEmail')!; }
  get currentPassword() { return this.form.get('currentPassword')!; }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  send(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close(this.form.getRawValue() as ChangeEmailDialogResult);
  }
}
