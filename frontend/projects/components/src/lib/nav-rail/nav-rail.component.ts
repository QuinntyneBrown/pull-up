import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { NavItem } from '../nav-item';

@Component({
  selector: 'pu-nav-rail',
  standalone: true,
  imports: [MatIconModule, RouterLink],
  templateUrl: './nav-rail.component.html',
  styleUrls: ['./nav-rail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavRailComponent {
  @Input() items: ReadonlyArray<NavItem> = [];
  @Input() activeRoute: string | null = null;
}
