// Traces to: L2-022 (list upcoming events), L2-024 (group by time period), L2-025 (filter chips),
//            L2-062 (empty state), L2-063 (error state).

import { expect, test } from '@playwright/test';
import { HomePagePom } from './pages/home-page';

const SEED_EVENTS = {
  thisWeek: [
    {
      id: '1',
      title: 'BBQ at the Park',
      startsAtUtc: new Date(Date.now() + 2 * 86400_000).toISOString(),
      location: 'Riverfront Park',
      status: 'Active',
      isHost: true,
      myRsvpStatus: 'Going',
    },
  ],
  laterThisMonth: [],
  nextMonth: [],
  past: [
    {
      id: '2',
      title: 'Game Night',
      startsAtUtc: new Date(Date.now() - 7 * 86400_000).toISOString(),
      location: "Rosa's house",
      status: 'Active',
      isHost: false,
      myRsvpStatus: 'Maybe',
    },
  ],
};

test.describe('Home list flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
    await page.route('**/api/users/me', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          userId: '1',
          email: 'rosa@example.com',
          fullName: 'Rosa Marquez',
          displayName: 'Rosa',
          role: 'User',
          createdAt: '2026-01-01T00:00:00Z',
        }),
      });
    });
  });

  test('renders grouped event cards', async ({ page }) => {
    await page.route('**/api/events*', async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SEED_EVENTS) });
    });

    const pom = new HomePagePom(page);
    await page.goto('/home');
    await pom.expectVisible();
    await expect(page.getByTestId('home-group-this-week')).toBeVisible();
    await expect(page.getByText('BBQ at the Park')).toBeVisible();
    await expect(page.getByTestId('home-group-past')).toBeVisible();
  });

  test('shows empty state when no events', async ({ page }) => {
    await page.route('**/api/events*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] }),
      });
    });

    await page.goto('/home');
    await expect(page.getByTestId('home-empty')).toBeVisible();
  });

  test('shows error state when fetch fails', async ({ page }) => {
    await page.route('**/api/events*', async route => {
      await route.fulfill({ status: 500, body: 'Internal Server Error' });
    });

    await page.goto('/home');
    await expect(page.getByTestId('home-error')).toBeVisible();
  });

  test('filter chip re-fetches with new scope', async ({ page }) => {
    const requests: string[] = [];
    await page.route('**/api/events*', async route => {
      requests.push(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] }),
      });
    });

    const pom = new HomePagePom(page);
    await page.goto('/home');
    await expect(pom.filterStrip).toBeVisible();
    await page.getByText('Hosting').click();
    await expect(page).toHaveURL(/\/home$/);
    expect(requests.length).toBeGreaterThanOrEqual(2);
  });
});
