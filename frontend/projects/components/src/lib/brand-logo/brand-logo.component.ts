import { Component, ChangeDetectionStrategy, Input } from '@angular/core';

@Component({
  selector: 'pu-brand-logo',
  standalone: true,
  templateUrl: './brand-logo.component.html',
  styleUrls: ['./brand-logo.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandLogoComponent {
  @Input() label: string = 'Pull Up';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
}
