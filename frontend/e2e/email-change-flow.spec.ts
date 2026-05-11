// Acceptance Test (Playwright POM)
// Traces to: L2-013 (email change — request a confirmation link, then confirm via the link to update the email).

import { expect, test } from '@playwright/test';
import { ConfirmEmailChangePagePom } from './pages/confirm-email-change-page';
import { ProfilePagePom } from './pages/profile-page';

const SEED_USER = {
  userId: '00000000-0000-0000-0000-000000000001',
  email: 'rosa@example.com',
  fullName: 'Rosa Marquez',
  displayName: 'Rosa',
  role: 'User',
  createdAt: '2026-01-01T00:00:00Z',
};

test.describe('Email change flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
    await page.route('**/api/users/me/notification-preferences', async route => {
      await route.fulfill({
        status: 200, contentType: 'application/json',
        body: JSON.stringify({ newInvitations: true, eventReminders: true, rsvpChanges: true }),
      });
    });
  });

  test('request: profile "Change email" dialog calls /api/users/me/email-change and shows verification snackbar', async ({ page }) => {
    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SEED_USER) });
      } else {
        await route.fallback();
      }
    });

    let requestSeen = false;
    await page.route('**/api/users/me/email-change', async route => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      expect(body).toEqual({ newEmail: 'rosa.new@example.com', currentPassword: 'Hunter2!secret' });
      requestSeen = true;
      await route.fulfill({ status: 202, body: '' });
    });

    const pom = new ProfilePagePom(page);
    await pom.goto();

    await page.getByTestId('profile-change-email-button').click();
    await expect(page.getByRole('heading', { name: 'Change email' })).toBeVisible();
    await page.getByTestId('change-email-new').fill('rosa.new@example.com');
    await page.getByTestId('change-email-password').fill('Hunter2!secret');
    await page.getByTestId('change-email-send').click();

    await expect(page.locator('.mat-mdc-snack-bar-container')).toContainText('Verification link sent');
    expect(requestSeen).toBe(true);
  });

  test('confirm: token + 204 → success state with link back to profile (which renders new email)', async ({ page }) => {
    let currentUser = { ...SEED_USER };
    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(currentUser) });
      } else {
        await route.fallback();
      }
    });
    await page.route('**/api/users/me/email-change/confirm', async route => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      expect(body).toEqual({ token: 'verify-token-123' });
      currentUser = { ...currentUser, email: 'rosa.new@example.com' };
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new ConfirmEmailChangePagePom(page);
    await pom.gotoWithToken('verify-token-123');
    await expect(pom.success).toContainText('Your account email has been updated');

    // Navigate to profile via the link in the success state; verify the new email is rendered.
    await pom.profileLink.click();
    await expect(page.getByTestId('profile-email')).toHaveValue('rosa.new@example.com');
  });

  test('confirm: 400 stale/invalid token → invalid state surface', async ({ page }) => {
    await page.route('**/api/users/me/email-change/confirm', async route => {
      await route.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ message: 'invalid' }) });
    });

    const pom = new ConfirmEmailChangePagePom(page);
    await pom.gotoWithToken('stale');
    await expect(pom.invalid).toContainText('invalid or has expired');
  });

  test('confirm: no token in URL → missing state surface', async ({ page }) => {
    const pom = new ConfirmEmailChangePagePom(page);
    await pom.gotoWithoutToken();
    await expect(page.getByRole('heading', { name: 'Missing token' })).toBeVisible();
    await expect(pom.missing).toContainText('needs a confirmation token');
  });
});
