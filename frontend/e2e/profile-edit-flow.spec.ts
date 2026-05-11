// Acceptance Test (Playwright POM)
// Traces to: L2-011 (view profile from /api/users/me), L2-012 (edit name + display name via PUT).

import { expect, test } from '@playwright/test';
import { ProfilePagePom } from './pages/profile-page';

const SEED_USER = {
  userId: '00000000-0000-0000-0000-000000000001',
  email: 'rosa@example.com',
  fullName: 'Rosa Marquez',
  displayName: 'Rosa',
  role: 'User',
  createdAt: '2026-01-01T00:00:00Z',
};

test.describe('Profile edit flow', () => {
  test('load /profile, edit full name, save, reload — new name shown', async ({ page }) => {
    // Seed token so /profile route is admitted by authGuard.
    await page.addInitScript(() => {
      localStorage.setItem('pullup.access-token', 'mock.access.token');
    });

    let currentUser = { ...SEED_USER };

    await page.route('**/api/users/me', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(currentUser),
        });
      } else {
        await route.fallback();
      }
    });

    await page.route('**/api/users/me/profile', async route => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      currentUser = { ...currentUser, fullName: body.fullName, displayName: body.displayName };
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new ProfilePagePom(page);
    await pom.goto();

    await expect(pom.fullName).toHaveValue('Rosa Marquez');
    await expect(pom.displayName).toHaveValue('Rosa');
    await expect(pom.email).toHaveValue('rosa@example.com');

    await pom.openEdit();
    await pom.editAndSave('Rosa M. Marquez', 'Rose');

    // After save the page re-fetches /api/users/me, which now returns the new name.
    await expect(pom.fullName).toHaveValue('Rosa M. Marquez');
    await expect(pom.displayName).toHaveValue('Rose');
  });
});
