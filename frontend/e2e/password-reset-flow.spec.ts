// Acceptance Test (Playwright POM)
// Traces to: L2-008 (request password reset — universal success state, no enumeration),
//            L2-009 (complete password reset — 204 then sign-in; 400 stale token),
//            L2-003 (password complexity rule — min 8 chars enforced client-side).

import { expect, test } from '@playwright/test';
import { CompletePasswordResetPagePom } from './pages/complete-password-reset-page';
import { RequestPasswordResetPagePom } from './pages/request-password-reset-page';

test.describe('Password reset flow', () => {
  test('request: shows universal success state regardless of email validity', async ({ page }) => {
    await page.route('**/api/auth/password-reset', async route => {
      await route.fulfill({ status: 202, contentType: 'application/json', body: '{}' });
    });

    const pom = new RequestPasswordResetPagePom(page);
    await pom.goto();
    await pom.fillAndSubmit('someone@example.com');

    await expect(pom.success).toContainText('If an account exists');
    await expect(pom.backLink).toBeVisible();
  });

  test('request: also shows universal success state on server error (no enumeration leak)', async ({ page }) => {
    await page.route('**/api/auth/password-reset', async route => {
      await route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
    });

    const pom = new RequestPasswordResetPagePom(page);
    await pom.goto();
    await pom.fillAndSubmit('unknown@example.com');

    await expect(pom.success).toContainText('If an account exists');
  });

  test('confirm: 204 navigates to /sign-in with success snackbar', async ({ page }) => {
    await page.route('**/api/auth/password-reset/confirm', async route => {
      await route.fulfill({ status: 204, body: '' });
    });

    const pom = new CompletePasswordResetPagePom(page);
    await pom.gotoWithToken('valid-token-123');
    await pom.fillAndSubmit('BrandNewPass1!');

    await expect(page).toHaveURL(/\/sign-in$/);
    await expect(page.locator('.mat-mdc-snack-bar-container')).toContainText('Password updated');
  });

  test('confirm: 400 stale token surfaces inline error and keeps the user on the page', async ({ page }) => {
    await page.route('**/api/auth/password-reset/confirm', async route => {
      await route.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ message: 'invalid' }) });
    });

    const pom = new CompletePasswordResetPagePom(page);
    await pom.gotoWithToken('stale-token');
    await pom.fillAndSubmit('BrandNewPass1!');

    await expect(pom.serverError).toContainText('invalid or has expired');
    await expect(page).toHaveURL(/\/password-reset\/confirm/);
  });
});
