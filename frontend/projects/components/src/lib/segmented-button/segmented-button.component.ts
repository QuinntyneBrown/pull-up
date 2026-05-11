import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface SegmentedOption<TValue extends string = string> {
  readonly value: TValue;
  readonly label: string;
  readonly icon?: string;
}

@Component({
  selector: 'pu-segmented-button',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './segmented-button.component.html',
  styleUrls: ['./segmented-button.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SegmentedButtonComponent {
  @Input() options: ReadonlyArray<SegmentedOption> = [];
  @Input() selected: string | null = null;
  @Input() ariaLabel: string = '';
  @Output() readonly selectionChange = new EventEmitter<string>();

  onSelect(value: string): void {
    if (value !== this.selected) {
      this.selectionChange.emit(value);
    }
  }
}
