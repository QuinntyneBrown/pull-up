// Visual snapshot helper — captures the real app rendered by the dev server
// at the mock breakpoints so the MF2 evaluator can compare them to D1's mocks.
// Runs as part of `npm run e2e`; outputs to docs/evaluations/MF2-screenshots/.

import { test } from '@playwright/test';
import path from 'path';

const OUT_DIR = path.resolve(__dirname, '..', '..', 'docs', 'evaluations', 'MF2-screenshots');

test.describe.configure({ mode: 'serial' });

test('snapshot sign-up at 360', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto('/sign-up');
  await page.getByRole('heading', { name: 'Create your account' }).waitFor();
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(OUT_DIR, 'sign-up.360.png'), fullPage: true });
});

test('snapshot sign-up at 768', async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto('/sign-up');
  await page.getByRole('heading', { name: 'Create your account' }).waitFor();
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(OUT_DIR, 'sign-up.768.png'), fullPage: true });
});

test('snapshot sign-up at 1440', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/sign-up');
  await page.getByRole('heading', { name: 'Create your account' }).waitFor();
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(OUT_DIR, 'sign-up.1440.png'), fullPage: true });
});

test('snapshot home at 360 (after sign-up)', async ({ page }) => {
  await page.route('**/api/users', async route => {
    if (route.request().method() !== 'POST') return route.fallback();
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
  await page.route('**/api/events*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] }),
    });
  });

  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto('/sign-up');
  await page.getByTestId('signup-fullname').fill('Rosa Marquez');
  await page.getByTestId('signup-email').fill('rosa@example.com');
  await page.getByTestId('signup-password').fill('Hunter2!secret');
  await page.getByTestId('signup-submit').click();
  await page.getByTestId('home-filter-strip').waitFor();
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(OUT_DIR, 'home.360.png'), fullPage: true });
});

test('snapshot home at 1440', async ({ page }) => {
  await page.route('**/api/users', async route => {
    if (route.request().method() !== 'POST') return route.fallback();
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
  await page.route('**/api/events*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ thisWeek: [], laterThisMonth: [], nextMonth: [], past: [] }),
    });
  });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/sign-up');
  await page.getByTestId('signup-fullname').fill('Rosa Marquez');
  await page.getByTestId('signup-email').fill('rosa@example.com');
  await page.getByTestId('signup-password').fill('Hunter2!secret');
  await page.getByTestId('signup-submit').click();
  await page.getByTestId('home-filter-strip').waitFor();
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(OUT_DIR, 'home.1440.png'), fullPage: true });
});
