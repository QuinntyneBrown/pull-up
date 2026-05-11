import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

export interface FilterChip {
  readonly key: string;
  readonly label: string;
}

@Component({
  selector: 'pu-filter-strip',
  standalone: true,
  templateUrl: './filter-strip.component.html',
  styleUrls: ['./filter-strip.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterStripComponent {
  @Input() chips: ReadonlyArray<FilterChip> = [];
  @Input() selectedKey: string | null = null;
  @Output() readonly chipChange = new EventEmitter<string>();

  onSelect(key: string): void {
    if (key !== this.selectedKey) {
      this.chipChange.emit(key);
    }
  }
}
