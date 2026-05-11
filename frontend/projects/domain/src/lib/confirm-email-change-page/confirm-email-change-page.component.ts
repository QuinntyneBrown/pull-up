import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IProfileService, PROFILE_SERVICE } from 'api';
import { BrandLogoComponent } from 'components';

@Component({
  selector: 'pu-confirm-email-change-page',
  standalone: true,
  imports: [CommonModule, MatButtonModule, BrandLogoComponent, RouterLink],
  templateUrl: './confirm-email-change-page.component.html',
  styleUrls: ['./confirm-email-change-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmEmailChangePageComponent implements OnInit {
  readonly state = signal<'pending' | 'success' | 'invalid' | 'missing'>('pending');

  constructor(
    private readonly route: ActivatedRoute,
    @Inject(PROFILE_SERVICE) private readonly profile: IProfileService,
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state.set('missing');
      return;
    }
    this.profile.confirmEmailChange({ token }).subscribe({
      next: () => this.state.set('success'),
      error: () => this.state.set('invalid'),
    });
  }
}
