import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { AUTH_SERVICE, CurrentUser, IAuthService } from 'api';
import { BrandLogoComponent } from 'components';

@Component({
  selector: 'pu-home-page',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, BrandLogoComponent],
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePageComponent implements OnInit {
  readonly user = signal<CurrentUser | null>(null);
  readonly loading = signal(true);
  readonly errored = signal(false);

  constructor(
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.auth.loadCurrentUser().subscribe({
      next: u => {
        this.user.set(u);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errored.set(true);
      },
    });
  }

  signOut(): void {
    this.auth.signOut().subscribe({
      next: () => this.router.navigateByUrl('/sign-up'),
      error: () => this.router.navigateByUrl('/sign-up'),
    });
  }
}
