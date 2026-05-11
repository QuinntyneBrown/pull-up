// Traces to: L2-028 (host edits title/date/time/location), L2-029 (description editable),
//            L2-031 (add invitee on edit), L2-032 (remove invitee from edit page).

import { expect, test } from '@playwright/test';
import { EventEditPagePom } from './pages/event-edit-page';

const EVENT_ID = 'evt-edit';
const FUTURE = new Date(Date.now() + 7 * 86400_000).toISOString();

function makeEvent(overrides: Record<string, unknown> = {}) {
  return {
    id: EVENT_ID,
    title: "Rosa's Cookout",
    startsAtUtc: FUTURE,
    endsAtUtc: null,
    location: 'Backyard',
    description: 'Bring sides.',
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

test.describe('Event edit flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
  });

  test('load, edit title, save navigates to detail', async ({ page }) => {
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 204, body: '' });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeEvent()),
      });
    });

    const pom = new EventEditPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.titleInput).toHaveValue("Rosa's Cookout");

    await pom.titleInput.fill('Updated Cookout');
    await pom.submitButton.click();

    await expect(page).toHaveURL(new RegExp(`/events/${EVENT_ID}$`));
  });

  test('add and remove pending invitee', async ({ page }) => {
    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 204, body: '' });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeEvent()),
      });
    });
    await page.route(`**/api/events/${EVENT_ID}/invitees`, async route => {
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new EventEditPagePom(page);
    await pom.goto(EVENT_ID);

    await pom.inviteeInput.fill('bob@example.com');
    await pom.inviteeAddButton.click();
    await expect(pom.pendingChips).toBeVisible();
    await expect(page.getByText('bob@example.com')).toBeVisible();

    await page.getByRole('button', { name: 'Remove bob@example.com' }).click();
    await expect(pom.pendingChips).not.toBeVisible();
  });

  test('existing guest chip removes invitee', async ({ page }) => {
    const invId = 'inv-001';

    await page.route(`**/api/events/${EVENT_ID}`, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(
          makeEvent({
            guests: [
              {
                userId: '2',
                email: 'bob@example.com',
                fullName: 'Bob Smith',
                displayName: 'Bob',
                rsvpStatus: null,
                note: null,
                invitationId: invId,
              },
            ],
          }),
        ),
      });
    });
    await page.route(`**/api/events/${EVENT_ID}/invitees/${invId}`, async route => {
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new EventEditPagePom(page);
    await pom.goto(EVENT_ID);

    await expect(pom.existingChips).toBeVisible();
    await page.getByRole('button', { name: 'Remove Bob' }).click();

    await expect(pom.existingChips).not.toBeVisible();
  });
});
