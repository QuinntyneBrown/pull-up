// Traces to: L2-014 (delete account removes personal data), L2-015 (cancels participation in future events).

import { expect, test } from '@playwright/test';
import { DeleteAccountPagePom } from './pages/delete-account-page';

const CURRENT_USER = {
  userId: '1',
  email: 'rosa@example.com',
  fullName: 'Rosa',
  displayName: 'Rosa',
  role: 'User',
  createdAt: '2026-01-01T00:00:00Z',
};

test.describe('Delete account flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });
  });

  test('wrong password shows error and stays on page', async ({ page }) => {
    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'DELETE') {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'wrong password' }),
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CURRENT_USER),
      });
    });

    const pom = new DeleteAccountPagePom(page);
    await pom.goto();
    await pom.fillAndSubmit('wrong-password');

    await expect(pom.serverError).toContainText('Current password is incorrect.');
    await expect(page).toHaveURL(/\/profile\/delete$/);
  });

  test('correct password navigates to /sign-up', async ({ page }) => {
    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'DELETE') {
        await route.fulfill({ status: 204, body: '' });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CURRENT_USER),
      });
    });

    const pom = new DeleteAccountPagePom(page);
    await pom.goto();
    await pom.fillAndSubmit('correct-password');

    await expect(page).toHaveURL(/\/sign-up$/);
  });
});
