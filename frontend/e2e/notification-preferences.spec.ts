// Acceptance Test (Playwright POM)
// Traces to: L2-016 (read notification preferences on mount),
//            L2-017 (update notification preferences with debounce, persists across reload).

import { expect, test } from '@playwright/test';

const SEED_USER = {
  userId: '00000000-0000-0000-0000-000000000001',
  email: 'rosa@example.com',
  fullName: 'Rosa Marquez',
  displayName: 'Rosa',
  role: 'User',
  createdAt: '2026-01-01T00:00:00Z',
};

test.describe('Notification preferences', () => {
  test('new user lands with all-on; toggling persists across reload', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });

    let prefs = { newInvitations: true, eventReminders: true, rsvpChanges: true };

    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SEED_USER) });
      } else {
        await route.fallback();
      }
    });

    await page.route('**/api/users/me/notification-preferences', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(prefs) });
        return;
      }
      if (route.request().method() === 'PUT') {
        const body = JSON.parse(route.request().postData() ?? '{}');
        prefs = { newInvitations: !!body.newInvitations, eventReminders: !!body.eventReminders, rsvpChanges: !!body.rsvpChanges };
        await route.fulfill({ status: 204, body: '' });
        return;
      }
      await route.fallback();
    });

    await page.goto('/profile');
    await expect(page.getByRole('heading', { name: 'Notifications' })).toBeVisible();

    const newInvitationsBtn = page.getByTestId('prefs-new-invitations').locator('button[role="switch"]');
    const eventRemindersBtn = page.getByTestId('prefs-event-reminders').locator('button[role="switch"]');
    const rsvpChangesBtn = page.getByTestId('prefs-rsvp-changes').locator('button[role="switch"]');

    // All-on by default for a new user.
    await expect(newInvitationsBtn).toHaveAttribute('aria-checked', 'true');
    await expect(eventRemindersBtn).toHaveAttribute('aria-checked', 'true');
    await expect(rsvpChangesBtn).toHaveAttribute('aria-checked', 'true');

    // Toggle event reminders off.
    await eventRemindersBtn.click();
    await expect(eventRemindersBtn).toHaveAttribute('aria-checked', 'false');

    // Wait for the debounced PUT to land.
    await page.waitForResponse(r => r.url().includes('/api/users/me/notification-preferences') && r.request().method() === 'PUT');

    // Reload — the server (test stub) now returns the persisted state.
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Notifications' })).toBeVisible();
    await expect(page.getByTestId('prefs-event-reminders').locator('button[role="switch"]')).toHaveAttribute('aria-checked', 'false');
    await expect(page.getByTestId('prefs-new-invitations').locator('button[role="switch"]')).toHaveAttribute('aria-checked', 'true');
    await expect(page.getByTestId('prefs-rsvp-changes').locator('button[role="switch"]')).toHaveAttribute('aria-checked', 'true');
  });
});
