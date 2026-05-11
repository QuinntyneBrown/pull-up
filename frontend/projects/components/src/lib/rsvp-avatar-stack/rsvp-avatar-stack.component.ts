import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type AvatarTone = 'primary' | 'secondary' | 'tertiary';

export interface AvatarEntry {
  readonly initials: string;
  readonly tone: AvatarTone;
}

@Component({
  selector: 'pu-rsvp-avatar-stack',
  standalone: true,
  templateUrl: './rsvp-avatar-stack.component.html',
  styleUrls: ['./rsvp-avatar-stack.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RsvpAvatarStackComponent {
  @Input() entries: ReadonlyArray<AvatarEntry> = [];
  @Input() maxVisible: number = 4;

  get visible(): ReadonlyArray<AvatarEntry> {
    return this.entries.slice(0, this.maxVisible);
  }

  get overflowCount(): number {
    return Math.max(0, this.entries.length - this.maxVisible);
  }
}
