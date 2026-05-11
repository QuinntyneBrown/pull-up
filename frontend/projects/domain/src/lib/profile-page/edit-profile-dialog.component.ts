import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface EditProfileDialogData {
  readonly fullName: string;
  readonly displayName: string;
}

export interface EditProfileDialogResult {
  readonly fullName: string;
  readonly displayName: string;
}

@Component({
  selector: 'pu-edit-profile-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './edit-profile-dialog.component.html',
  styleUrls: ['./edit-profile-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditProfileDialogComponent {
  readonly form: FormGroup;
  readonly submitting = signal(false);

  constructor(
    fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<EditProfileDialogComponent, EditProfileDialogResult | undefined>,
    @Inject(MAT_DIALOG_DATA) data: EditProfileDialogData,
  ) {
    this.form = fb.nonNullable.group({
      fullName: [data.fullName, [Validators.required, Validators.maxLength(100)]],
      displayName: [data.displayName, [Validators.required, Validators.maxLength(50)]],
    });
  }

  get fullName() { return this.form.get('fullName')!; }
  get displayName() { return this.form.get('displayName')!; }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close(this.form.getRawValue() as EditProfileDialogResult);
  }
}
