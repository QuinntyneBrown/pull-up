// Traces to: L2-026 (host cancels event), L2-027 (cancelled banner shown).

import { expect, test } from '@playwright/test';
import { EventDetailPagePom } from './pages/event-detail-page';

const EVENT_ID = 'evt-host';

function makeEvent(overrides: Record<string, unknown> = {}) {
  return {
    id: EVENT_ID,
    title: "Rosa's BBQ",
    startsAtUtc: new Date(Date.now() + 7 * 86400_000).toISOString(),
    endsAtUtc: null,
    location: 'The Park',
    description: '',
    status: 'Active',
    allowPlusOne: false,
    showGuestList: true,
    host: { userId: '1', fullName: 'Rosa Marquez', displayName: 'Rosa', email: 'rosa@example.com' },
    isHost: true,
    myRsvpStatus: 'Going',
    goingCount: 1,
    maybeCount: 0,
    cantGoCount: 0,
    guests: [],
    ...overrides,
  };
}

test.describe('Event cancel flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
  });

  test('host cancels event — cancelled banner appears', async ({ page }) => {
    let callCount = 0;
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      callCount += 1;
      const status = callCount > 1 ? 'Cancelled' : 'Active';
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeEvent({ status })),
      });
    });
    await page.route(`**/api/events/${EVENT_ID}/cancel`, async route => {
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new EventDetailPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.cancelButton).toBeVisible();
    await pom.cancelButton.click();

    await expect(page.getByTestId('cancel-dialog-confirm')).toBeVisible();
    await page.getByTestId('cancel-dialog-confirm').click();

    await expect(pom.cancelledBanner).toBeVisible();
    await expect(pom.cancelButton).not.toBeVisible();
  });

  test('guest does not see cancel button', async ({ page }) => {
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeEvent({ isHost: false })),
      });
    });

    const pom = new EventDetailPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.cancelButton).not.toBeVisible();
  });
});
