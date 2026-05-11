import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'pu-loading-skeleton',
  standalone: true,
  templateUrl: './loading-skeleton.component.html',
  styleUrls: ['./loading-skeleton.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingSkeletonComponent {
  @Input() count: number = 3;
  @Input() height: string = '88px';

  get rows(): ReadonlyArray<number> {
    return Array.from({ length: Math.max(1, this.count) }, (_, i) => i);
  }
}
