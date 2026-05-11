import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'pu-error-state',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './error-state.component.html',
  styleUrls: ['./error-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorStateComponent {
  @Input() icon: string = 'error_outline';
  @Input() title: string = 'Something went wrong';
  @Input() supportingText: string = "We couldn't load this. Please try again.";
  @Input() retryLabel: string = 'Try again';
  @Output() readonly retry = new EventEmitter<void>();

  onRetry(): void {
    this.retry.emit();
  }
}
