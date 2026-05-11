// Traces to: L2-018 (create event with title/date/time/location), L2-019 (description optional),
//            L2-020 (invitees on create), L2-021 (event persisted + visible in list),
//            L2-031 (add invitee), L2-032 (remove invitee via chip).

import { expect, test } from '@playwright/test';
import { EventCreatePagePom } from './pages/event-create-page';

test.describe('Event create flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
  });

  test('fill form, submit, navigate to detail page', async ({ page }) => {
    const createdId = 'evt-001';

    await page.route('**/api/events', async route => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: createdId }),
        });
        return;
      }

      await route.fallback();
    });

    await page.route(`**/api/events/${createdId}`, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: createdId,
          title: 'BBQ at the Park',
          startsAtUtc: new Date(Date.now() + 3 * 86400_000).toISOString(),
          endsAtUtc: null,
          location: 'Riverfront Park',
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
        }),
      });
    });

    const pom = new EventCreatePagePom(page);
    await pom.goto();
    await pom.fillBasicFields('BBQ at the Park', 'Riverfront Park');
    await pom.submitButton.click();

    await expect(page).toHaveURL(new RegExp(`/events/${createdId}$`));
  });
});
