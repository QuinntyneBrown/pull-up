import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { NavItem } from '../nav-item';

@Component({
  selector: 'pu-bottom-nav-bar',
  standalone: true,
  imports: [MatIconModule, RouterLink],
  templateUrl: './bottom-nav-bar.component.html',
  styleUrls: ['./bottom-nav-bar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BottomNavBarComponent {
  @Input() items: ReadonlyArray<NavItem> = [];
  @Input() activeRoute: string | null = null;
}
