// Acceptance Test (Playwright POM)
// Traces to: L2-004 (sign-in valid credentials -> 200 + tokens), L2-005 (invalid credentials -> 401 generic message).

import { expect, test } from '@playwright/test';
import { HomePagePom } from './pages/home-page';
import { SignInPagePom } from './pages/sign-in-page';

test.describe('Pull Up sign-in flow', () => {
  test('happy path: stores tokens and navigates to home', async ({ page }) => {
    await page.route('**/api/auth/sign-in', async route => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          userId: '00000000-0000-0000-0000-000000000001',
          email: body.email,
          fullName: 'Rosa Marquez',
          displayName: 'Rosa',
          role: 'User',
          accessToken: 'mock.access.token',
          accessTokenExpiresAt: new Date(Date.now() + 60 * 60_000).toISOString(),
          refreshToken: 'mock.refresh.token',
          refreshTokenExpiresAt: new Date(Date.now() + 7 * 24 * 60 * 60_000).toISOString(),
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

    const signIn = new SignInPagePom(page);
    const home = new HomePagePom(page);

    await signIn.goto();
    await signIn.fillAndSubmit('rosa@example.com', 'Hunter2!secret');

    await home.expectVisible('Rosa');

    // Tokens stored.
    const tokens = await page.evaluate(() => ({
      access: localStorage.getItem('pullup.access-token'),
      refresh: localStorage.getItem('pullup.refresh-token'),
    }));
    expect(tokens.access).toBe('mock.access.token');
    expect(tokens.refresh).toBe('mock.refresh.token');
  });

  test('invalid credentials: shows generic 401 message and stays on /sign-in', async ({ page }) => {
    await page.route('**/api/auth/sign-in', async route => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'invalid' }),
      });
    });

    const signIn = new SignInPagePom(page);
    await signIn.goto();
    await signIn.fillAndSubmit('rosa@example.com', 'wrong-password');

    await expect(signIn.serverError).toHaveText('Invalid email or password.');
    await expect(page).toHaveURL(/\/sign-in$/);
  });
});
