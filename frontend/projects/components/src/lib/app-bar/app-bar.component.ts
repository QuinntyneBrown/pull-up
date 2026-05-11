import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'pu-app-bar',
  standalone: true,
  templateUrl: './app-bar.component.html',
  styleUrls: ['./app-bar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppBarComponent {
  @Input() title: string = '';
}
