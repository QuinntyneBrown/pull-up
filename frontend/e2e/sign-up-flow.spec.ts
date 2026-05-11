// Acceptance Test (Playwright POM)
// Traces to: L2-001 (register valid input -> 201 + token), L2-022 (home rendered after auth),
// L2-052 (mobile vs desktop navigation breakpoint).
// Description: full happy-path flow — open sign-up, fill the form, submit, get a JWT,
// land on /home with the user profile loaded from /api/users/me.

import { expect, test } from '@playwright/test';
import { HomePagePom } from './pages/home-page';
import { SignUpPagePom } from './pages/sign-up-page';

test.describe('Pull Up MVP sign-up + home', () => {
  test.beforeEach(async ({ page }) => {
    // Mock backend so the test runs without a running .NET API.
    await page.route('**/api/users', async route => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      const body = JSON.parse(route.request().postData() ?? '{}');
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          userId: '00000000-0000-0000-0000-000000000001',
          email: body.email,
          fullName: body.fullName,
          displayName: String(body.fullName).split(' ')[0],
          accessToken: 'mock.jwt.token',
          accessTokenExpiresAt: new Date(Date.now() + 60 * 60_000).toISOString(),
        }),
      });
    });

    await page.route('**/api/users/me', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          userId: '00000000-0000-0000-0000-000000000001',
          email: 'rosa@example.com',
          fullName: 'Rosa Marquez',
          displayName: 'Rosa',
          role: 'User',
          createdAt: new Date().toISOString(),
        }),
      });
    });
    await page.route('**/api/events', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] }),
      });
    });
  });

  test('signs up and lands on home with user profile rendered', async ({ page }) => {
    const signUp = new SignUpPagePom(page);
    const home = new HomePagePom(page);

    await signUp.goto();
    await signUp.fillAndSubmit('Rosa Marquez', 'rosa@example.com', 'Hunter2!secret');

    await home.expectVisible();
  });

  test('renders the sign-up shell at 360px width (mobile-first)', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });

    const signUp = new SignUpPagePom(page);
    await signUp.goto();

    await expect(signUp.fullName).toBeVisible();
    await expect(signUp.email).toBeVisible();
    await expect(signUp.password).toBeVisible();
    await expect(signUp.submit).toBeVisible();

    // No horizontal scroll at 360px (mobile-first responsive — L2-054).
    const dims = await page.evaluate(() => ({
      scroll: document.documentElement.scrollWidth,
      client: document.documentElement.clientWidth,
    }));
    expect(dims.scroll).toBeLessThanOrEqual(dims.client);
  });
});
