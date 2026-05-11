// Traces to: L2-060, L2-061, L2-062 (responsive layout at mobile/tablet/desktop).
// Tests that key page elements are visible at 360px, 768px, and 1440px viewport widths.

import { expect, test } from '@playwright/test';

const VIEWPORTS = [
  { name: 'mobile', width: 360, height: 780 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1440, height: 900 },
];

const EVENT_ID = 'evt-resp';
const FUTURE = new Date(Date.now() + 7 * 86400_000).toISOString();

function makeEventList() {
  return { thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] };
}

function makeEvent() {
  return {
    id: EVENT_ID,
    title: 'Responsive Test Event',
    startsAtUtc: FUTURE,
    endsAtUtc: null,
    location: 'Test Venue',
    description: '',
    status: 'Active',
    allowPlusOne: false,
    showGuestList: true,
    host: { userId: '1', fullName: 'Rosa Marquez', displayName: 'Rosa', email: 'rosa@example.com' },
    isHost: false,
    myRsvpStatus: null,
    goingCount: 0,
    maybeCount: 0,
    cantGoCount: 0,
    guests: [],
  };
}

for (const vp of VIEWPORTS) {
  test.describe(`Responsive — ${vp.name} (${vp.width}px)`, () => {
    test.use({ viewport: { width: vp.width, height: vp.height } });

    test.beforeEach(async ({ page }) => {
      await page.addInitScript(() => {
        localStorage.setItem('pullup.access-token', 'mock.access.token');
      });
    });

    test('home page renders filter strip and FAB', async ({ page }) => {
      await page.route('**/api/events', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(makeEventList()),
        });
      });

      await page.goto('/home');
      await expect(page.getByTestId('home-filter-strip')).toBeVisible();
      await expect(page.getByTestId('home-create-fab')).toBeVisible();
    });

    test('event detail page renders hero section', async ({ page }) => {
      await page.route(`**/api/events/${EVENT_ID}`, async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(makeEvent()),
        });
      });

      await page.goto(`/events/${EVENT_ID}`);
      await expect(page.getByTestId('detail-hero')).toBeVisible();
      await expect(page.getByTestId('detail-title')).toBeVisible();
    });

    test('event create page renders form', async ({ page }) => {
      await page.goto('/events/new');
      await expect(page.getByTestId('create-title')).toBeVisible();
      await expect(page.getByTestId('create-submit')).toBeVisible();
    });

    test('profile page renders', async ({ page }) => {
      await page.route('**/api/users/me', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            userId: '1',
            email: 'rosa@example.com',
            fullName: 'Rosa Marquez',
            displayName: 'Rosa',
          }),
        });
      });
      await page.route('**/api/users/me/notification-preferences', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ emailOnInvitation: true, emailOnRsvpChange: false }),
        });
      });

      await page.goto('/profile');
      await expect(page.getByTestId('profile-fullname')).toBeVisible();
    });
  });
}
