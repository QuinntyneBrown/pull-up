import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { EVENTS_SERVICE, IEventsService, ListMyEventsResponse } from 'api';
import {
  AppBarComponent,
  BottomNavBarComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  EventCardComponent,
  FilterChip,
  FilterStripComponent,
  LoadingSkeletonComponent,
  NavItem,
  NavRailComponent,
} from 'components';

const NAV_ITEMS: ReadonlyArray<NavItem> = [
  { route: '/home', icon: 'event', label: 'Events' },
  { route: '/profile', icon: 'person', label: 'Profile' },
];

const FILTER_CHIPS: ReadonlyArray<FilterChip> = [
  { key: 'all', label: 'All' },
  { key: 'hosting', label: 'Hosting' },
  { key: 'invited', label: 'Invited' },
  { key: 'past', label: 'Past' },
];

@Component({
  selector: 'pu-home-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    AppBarComponent,
    BottomNavBarComponent,
    NavRailComponent,
    FilterStripComponent,
    EventCardComponent,
    LoadingSkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePageComponent implements OnInit {
  readonly navItems = NAV_ITEMS;
  readonly activeRoute = '/home';
  readonly filterChips = FILTER_CHIPS;
  readonly selectedScope = signal<string>('all');
  readonly loading = signal(true);
  readonly errored = signal(false);
  readonly events = signal<ListMyEventsResponse | null>(null);

  constructor(@Inject(EVENTS_SERVICE) private readonly eventsService: IEventsService) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  onScopeChange(key: string): void {
    this.selectedScope.set(key);
    this.loadEvents();
  }

  toDate(iso: string): Date {
    return new Date(iso);
  }

  get hasAnyEvents(): boolean {
    const events = this.events();
    if (!events) {
      return false;
    }

    return events.thisWeek.length > 0
      || events.laterThisMonth.length > 0
      || events.nextMonth.length > 0
      || events.past.length > 0;
  }

  retry(): void {
    this.loadEvents();
  }

  private loadEvents(): void {
    this.loading.set(true);
    this.errored.set(false);
    const scope = this.selectedScope() === 'all' ? null : this.selectedScope();

    this.eventsService.list(scope).subscribe({
      next: response => {
        this.events.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errored.set(true);
      },
    });
  }
}
