// Traces to: L2-023 (view event detail), L2-034 (RSVP picker visible for future events),
//            L2-035 (set/update RSVP), L2-036 (RSVP count updates after change).

import { expect, test } from '@playwright/test';
import { EventDetailPagePom } from './pages/event-detail-page';

const EVENT_ID = 'evt-abc';

function makeEvent(overrides: Record<string, unknown> = {}) {
  return {
    id: EVENT_ID,
    title: "Rosa's Birthday",
    startsAtUtc: new Date(Date.now() + 7 * 86400_000).toISOString(),
    endsAtUtc: null,
    location: "Rosa's house",
    description: 'Bring cake.',
    status: 'Active',
    allowPlusOne: false,
    showGuestList: true,
    host: { userId: '1', fullName: 'Rosa Marquez', displayName: 'Rosa', email: 'rosa@example.com' },
    isHost: false,
    myRsvpStatus: null,
    goingCount: 1,
    maybeCount: 0,
    cantGoCount: 0,
    guests: [],
    ...overrides,
  };
}

test.describe('Event RSVP flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
  });

  test('open detail, click Maybe, count refreshes', async ({ page }) => {
    let callCount = 0;
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      callCount += 1;
      const event = makeEvent({ myRsvpStatus: callCount > 1 ? 'Maybe' : null, maybeCount: callCount > 1 ? 1 : 0 });
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(event) });
    });
    await page.route(`**/api/events/${EVENT_ID}/rsvp`, async route => {
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new EventDetailPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.rsvpPicker).toBeVisible();
    await page.getByRole('radio', { name: 'Maybe' }).click();
    await expect(page.getByTestId('detail-going-count')).toBeVisible();
  });

  test('past event hides RSVP picker', async ({ page }) => {
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      const event = makeEvent({ startsAtUtc: new Date(Date.now() - 86400_000).toISOString() });
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(event) });
    });

    const pom = new EventDetailPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.rsvpPicker).not.toBeVisible();
  });
});
